using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using aspnetproject.Hubs;
using aspnetproject.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace aspnetproject.IntegrationTests.Helpers;

// A real SignalR client connected to the test host as a seeded user. It records every event in HubEvents.All
// from the moment it connects, so a test acts first (usually through HTTP as another user) and then reads what arrived.
public sealed class HubTestClient : IAsyncDisposable
{
    private const string HubPath          = "hubs/messages";
    private const string AccessTokenQuery = "access_token";

    // Deliberately not a hub method. The server answers a call to it with a "method does not exist" error,
    // and that reply is all ConfirmJoinedAsync needs.
    private const string RoundTripMethod = "TestRoundTrip";

    // Not a server event: the helper pushes it to this one connection to learn that every earlier event has been recorded.
    private const string MarkerEvent = "TestMarker";

    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly HubConnection                            _connection;
    private readonly IHubContext<MessageHub>                  _hubContext;
    private readonly string                                   _username;
    private readonly Dictionary<string, Channel<JsonElement>> _received = new();
    private readonly Channel<string>                          _markers  = Channel.CreateUnbounded<string>();

    private HubTestClient(HubConnection connection, IHubContext<MessageHub> hubContext, string username)
    {
        _connection = connection;
        _hubContext = hubContext;
        _username   = username;

        foreach (var eventName in HubEvents.All)
        {
            var channel = Channel.CreateUnbounded<JsonElement>();
            _received[eventName] = channel;

            // Every server event carries a single payload; it is kept as raw JSON and typed when the test reads it.
            _connection.On(eventName, [typeof(JsonElement)], args =>
            {
                channel.Writer.TryWrite((JsonElement)args[0]!);
                return Task.CompletedTask;
            });
        }

        _connection.On<string>(MarkerEvent, marker => _markers.Writer.TryWrite(marker));
    }

    // Signs in through the login endpoint and connects the way a browser client does: the token goes in a header
    // on the negotiate request and in the query string on the WebSocket, which cannot carry headers.
    public static async Task<HubTestClient> ConnectAsync(CustomWebApplicationFactory factory, string username)
    {
        using var httpClient = factory.CreateClient();
        var accessToken      = await AuthTestHelper.LoginAsync(httpClient, username, TestDataSeeder.SharedPlaintextPassword);

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, HubPath), options =>
            {
                options.Transports                = HttpTransportType.WebSockets;
                options.AccessTokenProvider       = () => Task.FromResult<string?>(accessToken);
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.WebSocketFactory          = (context, cancellationToken) =>
                    new ValueTask<WebSocket>(factory.Server.CreateWebSocketClient()
                        .ConnectAsync(WithAccessToken(context.Uri, accessToken), cancellationToken));
            })
            .Build();

        var client = new HubTestClient(connection, factory.Services.GetRequiredService<IHubContext<MessageHub>>(), username);
        await connection.StartAsync();
        await client.ConfirmJoinedAsync();

        return client;
    }

    // Returns the oldest unread payload of the event, waiting up to EventTimeout for one to arrive.
    public async Task<T> WaitForEventAsync<T>(string eventName)
    {
        var       channel = ReceivedFor(eventName);
        using var timeout = new CancellationTokenSource(EventTimeout);

        JsonElement payload = default;
        bool        arrived = false;

        try
        {
            payload = await channel.Reader.ReadAsync(timeout.Token);
            arrived = true;
        }
        catch (OperationCanceledException)
        {
            // Reported by the assertion below.
        }

        Assert.True(arrived, $"'{_username}' did not receive '{eventName}' within {EventTimeout.TotalSeconds} seconds.");

        return payload.Deserialize<T>(JsonSerializerOptions.Web)!;
    }

    // Fails if the event has arrived and not been read. Call it after the action that must not reach this user;
    // there is no fixed wait, see AwaitEarlierEventsAsync.
    public async Task AssertNoEventAsync(string eventName)
    {
        var channel = ReceivedFor(eventName);

        await AwaitEarlierEventsAsync();

        bool received = channel.Reader.TryRead(out var payload);
        Assert.False(received, $"'{_username}' received '{eventName}' but should not have. Payload: {payload}");
    }

    // Calls a hub method as this user and waits up to EventTimeout for the server to finish it.
    public async Task InvokeAsync(string methodName, params object?[] args)
    {
        using var timeout = new CancellationTokenSource(EventTimeout);

        await _connection.InvokeCoreAsync(methodName, args, timeout.Token);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    // The server confirms a connection before OnConnectedAsync has added it to the user's group, and reads the
    // client's calls only after OnConnectedAsync has finished. So the reply to any call means pushes to the user now reach this client.
    private async Task ConfirmJoinedAsync()
    {
        using var timeout = new CancellationTokenSource(EventTimeout);

        try
        {
            await _connection.InvokeAsync(RoundTripMethod, timeout.Token);
        }
        catch (HubException)
        {
            // Expected: the method does not exist. The reply arriving is the point.
        }
    }

    // Pushes a marker to this connection and waits for it. Anything the server sent this connection earlier (an HTTP
    // request that pushes has finished sending by the time its response arrives) is ahead of the marker on the wire,
    // and the client runs event handlers one at a time in arrival order, so by the time the marker arrives those events are recorded.
    private async Task AwaitEarlierEventsAsync()
    {
        var       marker  = Guid.NewGuid().ToString();
        using var timeout = new CancellationTokenSource(EventTimeout);

        await _hubContext.Clients.Client(_connection.ConnectionId!).SendAsync(MarkerEvent, marker, timeout.Token);

        string? arrivedMarker = null;

        try
        {
            arrivedMarker = await _markers.Reader.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            // Reported by the assertion below.
        }

        Assert.True(arrivedMarker == marker, $"'{_username}' did not receive the helper's marker within {EventTimeout.TotalSeconds} seconds.");
    }

    private Channel<JsonElement> ReceivedFor(string eventName)
    {
        bool isRecorded = _received.TryGetValue(eventName, out var channel);
        Assert.True(isRecorded, $"'{eventName}' is not recorded. Add it to {nameof(HubEvents)}.{nameof(HubEvents.All)}.");

        return channel!;
    }

    private static Uri WithAccessToken(Uri socketUri, string accessToken)
    {
        var builder = new UriBuilder(socketUri);
        builder.Query = $"{builder.Query.TrimStart('?')}&{AccessTokenQuery}={Uri.EscapeDataString(accessToken)}";

        return builder.Uri;
    }
}
