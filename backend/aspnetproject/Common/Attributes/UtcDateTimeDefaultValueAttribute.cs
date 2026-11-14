using System.ComponentModel;
using System.Globalization;

namespace aspnetproject.Common.Attributes;

// Default value for a DateTime property, given as an ISO 8601 UTC text and kept as a UTC DateTime.
// A plain text default makes the built-in OpenAPI generator throw, and parsing it into a DateTime without UTC
// handling would shift the documented example with the server's time zone.
[AttributeUsage(AttributeTargets.Property)]
public class UtcDateTimeDefaultValueAttribute(string isoUtcText)
    : DefaultValueAttribute(DateTime.Parse(isoUtcText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal));
