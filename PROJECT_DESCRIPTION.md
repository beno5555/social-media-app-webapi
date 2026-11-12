# WebCity — სოციალური ქსელის პლატფორმა

## პროექტის მიზანი

პროექტის მიზანია შეიქმნას თანამედროვე ვებ-აპლიკაცია, რომელიც მომხმარებლებს საშუალებას მისცემს ერთმანეთთან დაკავშირების, კონტენტის გაზიარებისა და რეალურ დროში კომუნიკაციისთვის. მომხმარებლებს შეეძლებათ ტექსტური და ფოტო პოსტების გამოქვეყნება, მოწონება და კომენტირება, მეგობრების დამატება და პირადი შეტყობინებების (მათ შორის ფოტოების) გაცვლა. სისტემა უზრუნველყოფს შეტყობინებებისა და მომხმარებლის ონლაინ სტატუსის რეალურ დროში მიწოდებას. ადმინისტრატორს შეეძლება მომხმარებლებისა და კონტენტის მართვა, ხოლო კონტენტის მოდერაცია, ელფოსტით კომუნიკაცია და სისტემის მონიტორინგი ავტომატიზებულია n8n-ის workflow-ებით.

სისტემა უნდა იყოს მარტივი მოხმარებისთვის და უზრუნველყოფდეს მონაცემების უსაფრთხო შენახვასა და დამუშავებას.

## ტექნიკური მოთხოვნები

### Back-end:

- პროგრამირების ენა: C#
- Framework: ASP.NET Core Web API (.NET 10)
- მონაცემთა ბაზა: SQL (SQL Server, Entity Framework Core)
- რეალურ დროში კომუნიკაცია: SignalR
- ავტომატიზაცია: n8n (self-hosted, Docker)

### Front-end:

- Framework: Angular

## მომხმარებელთა როლები:

- ადმინისტრატორი
- მომხმარებელი
- სტუმარი (არაავტორიზებული ვიზიტორი)

## მთავარი ფუნქციონალი:

### ავტორიზაციის სისტემა:

- მომხმარებლის რეგისტრაცია და ავტორიზაცია
- სესიის ავტომატური განახლება (access და refresh ტოკენები)
- პაროლის აღდგენა ელფოსტით
- ანგარიშის დეაქტივაცია და ხელახალი გააქტიურება
- ანგარიშის წაშლა

სისტემაში ავტორიზაცია და უსაფრთხოება უზრუნველყოფილია JWT ტოკენებით და როლებზე დაფუძნებული წვდომით, სადაც თითოეულ მომხმარებელს აქვს მკაფიოდ განსაზღვრული უფლებები. პაროლები ინახება დაშიფრული (hash) სახით. პაროლის აღდგენისას მომხმარებელი ელფოსტაზე იღებს დროებით ბმულს ან კოდს. ანგარიშის დეაქტივაციის შემდეგ მომხმარებელს შეუძლია ის ელფოსტით მიღებული კოდით აღადგინოს, ხოლო წაშლილი ანგარიშის მონაცემები გარკვეული პერიოდის შემდეგ ავტომატურად იშლება. უსაფრთხოებასთან დაკავშირებულ ყველა ცვლილებაზე მომხმარებელი იღებს შეტყობინებას ელფოსტაზე.

### პოსტები და კომენტარები:

- პოსტის შექმნა, რედაქტირება და წაშლა (სათაური, ტექსტი, ფოტოები)
- ერთ პოსტზე რამდენიმე ფოტოს ატვირთვა
- პოსტების მოწონება (like)
- პოსტების კომენტირება
- კომენტარის რედაქტირება და წაშლა
- პერსონალიზებული feed

პოსტების მოდული წარმოადგენს პლატფორმის ცენტრალურ ნაწილს. მომხმარებელს შეუძლია გამოაქვეყნოს ტექსტური პოსტი, ფოტოები ან ორივე ერთად. ფოტოები მოწმდება ფორმატისა და ზომის მიხედვით და ინახება სერვერზე, ბაზაში კი მხოლოდ მათი მისამართი იწერება. ნებისმიერი მომხმარებლის პროფილზე მისი პოსტები ყველასთვის ხელმისაწვდომია. პოსტის ავტორს შეუძლია წაშალოს ნებისმიერი კომენტარი საკუთარ პოსტზე. როდესაც ვინმე აკომენტარებს ან მოიწონებს პოსტს, ავტორი შეტყობინებას რეალურ დროში იღებს. ყველა სია გვერდებად (pagination) იტვირთება.

feed აჩვენებს როგორც მეგობრების, ისე სხვა მომხმარებლების პოსტებს. პოსტების რიგითობას განსაზღვრავს რეიტინგის ალგორითმი, რომელიც ითვალისწინებს:

- ავტორთან კავშირს (მეგობრების პოსტები პრიორიტეტულია, შემდეგ საერთო მეგობრების მქონე მომხმარებლების პოსტები)
- პოსტის აქტიურობას (მოწონებებისა და კომენტარების რაოდენობა)
- პოსტის სიახლეს (დროთა განმავლობაში პოსტის რეიტინგი მცირდება)

ასე მომხმარებელი ხედავს მეგობრების სიახლეებს და ამავდროულად აღმოაჩენს პოპულარულ კონტენტს და ახალ ადამიანებს.

### მეგობრობის სისტემა:

- მეგობრობის მოთხოვნის გაგზავნა
- მოთხოვნის მიღება ან უარყოფა
- მეგობრის წაშლა
- მეგობრების, შემოსული და გაგზავნილი მოთხოვნების სიები
- მომხმარებლების ძიება სახელით

მეგობრობის სისტემა განსაზღვრავს, ვისთან შეუძლია მომხმარებელს მიმოწერა და ვისი პოსტები მიიღებს პრიორიტეტს feed-ში. უარყოფილი მოთხოვნის შემდეგ შესაძლებელია ხელახალი მოთხოვნის გაგზავნა. მეგობრის წაშლისას მასთან მიმოწერის ისტორიაც იშლება. ნებისმიერი მომხმარებლის პროფილზე ჩანს მისი მეგობრების სია.

### პირადი შეტყობინებები (ჩატი):

- ტექსტური შეტყობინებებისა და ფოტოების გაგზავნა მეგობრებთან რეალურ დროში
- შეტყობინების რედაქტირება (გაგზავნიდან 20 წუთის განმავლობაში) და წაშლა
- წაკითხვის სტატუსი (seen)
- წაუკითხავი საუბრების რაოდენობა
- მეგობრების ონლაინ / ოფლაინ სტატუსი
- ბეჭდვის ინდიკატორი ("typing...")

ჩატის მოდული აგებულია SignalR-ზე, რაც შეტყობინებების მყისიერ მიწოდებას უზრუნველყოფს გვერდის განახლების გარეშე. შეტყობინება შეიძლება შეიცავდეს ტექსტს, ერთ ან რამდენიმე ფოტოს, ან ორივეს ერთად; ფოტოები მოწმდება ფორმატისა და ზომის მიხედვით. საუბრის გახსნისას შეტყობინებები ავტომატურად წაკითხულად ინიშნება და გამგზავნი ამის შესახებ შეტყობინებას იღებს. როდესაც მეგობარი წერს, მომხმარებელი ხედავს "typing..." ინდიკატორს. მომხმარებელი ხედავს, რომელი მეგობარია ონლაინ და როდის იყო ბოლოს აქტიური. საუბრების სიაში თითოეულ მეგობართან ჩანს ბოლო შეტყობინება.

### მომხმარებლის ინტერფეისი:

- მთავარი გვერდი feed-ით
- პოსტის შექმნა ფოტოების ატვირთვით
- პროფილის გვერდი (ბიოგრაფია, პოსტები, მეგობრები)
- პროფილის რედაქტირება
- მომხმარებლების ძიება
- ჩატის გვერდი საუბრების სიით
- შეტყობინებები რეალურ დროში (ახალი შეტყობინება, კომენტარი, მოწონება, მეგობრის ონლაინ სტატუსი)
- ადმინისტრატორის პანელის გვერდები

Angular აპლიკაცია დაყოფილია ფუნქციურ ნაწილებად (ავტორიზაცია, feed, პროფილი, ჩატი, ადმინისტრატორის პანელი), რომლებიც საჭიროებისამებრ იტვირთება, ხოლო საერთო კომპონენტები და სერვისები ცალკეა გამოტანილი. აპლიკაცია მოიცავს ავტორიზაციის გვერდებს (რეგისტრაცია, შესვლა, პაროლის აღდგენა, ანგარიშის აღდგენა) და დაცულ გვერდებს, რომლებზეც წვდომა მხოლოდ ავტორიზებულ მომხმარებელს აქვს, ხოლო ადმინისტრატორის პანელზე — მხოლოდ ადმინისტრატორს. ფორმები მოწმდება კლიენტის მხარესაც. HTTP interceptor ყველა მოთხოვნას ტოკენს ამატებს და ვადის გასვლისას ავტომატურად ანახლებს მას. აპლიკაციის მდგომარეობა (ავტორიზებული მომხმარებელი, ჩატი, ონლაინ მეგობრები, წაუკითხავი შეტყობინებები) იმართება ცენტრალიზებულად (Angular signals / services), რათა SignalR-ით მიღებული ცვლილებები ყველა კომპონენტში ერთდროულად აისახოს. დიზაინი რესპონსიულია და მუშაობს როგორც კომპიუტერზე, ისე მობილურზე.

### ადმინისტრატორის პანელი:

- მომხმარებლების მართვა (შექმნა, რედაქტირება, დეაქტივაცია, აღდგენა, წაშლა)
- ადმინისტრატორის როლის მინიჭება და მოხსნა
- ნებისმიერი პოსტის და კომენტარის წაშლა
- სისტემური ლოგების ნახვა
- სისტემის სტატისტიკის ნახვა
- მომხმარებლების რეპორტების განხილვა
- AI მოდერაციის მიერ მონიშნული კონტენტის განხილვა

ადმინისტრატორს შეუძლია მართოს ნებისმიერი მომხმარებლის ანგარიში და წაშალოს წესების დამრღვევი კონტენტი. სისტემა ბაზაში ინახავს ყველა მნიშვნელოვანი მოქმედების ჩანაწერს (ვინ, რა მოქმედება, წარმატებით თუ არა), რაც ადმინისტრატორს საშუალებას აძლევს თვალი ადევნოს სისტემის მუშაობას. სტატისტიკის გვერდზე ჩანს მომხმარებლების, პოსტებისა და შეტყობინებების რაოდენობა და აქტიურობა. ძველი ლოგები და ვადაგასული ტოკენები პერიოდულად, background სერვისების გამოყენებით იშლება.

მომხმარებელს შეუძლია დაარეპორტოს პოსტი, კომენტარი ან სხვა მომხმარებელი, მიზეზის მითითებით. ადმინისტრატორი ხედავს რეპორტების სიას, ამოწმებს კონტენტს და იღებს გადაწყვეტილებას: კონტენტის დამალვა ან რეპორტის უარყოფა. დამალული კონტენტი სხვა მომხმარებლებისთვის აღარ ჩანს.

### n8n ავტომატიზაცია:

- კონტენტის AI მოდერაცია (პოსტები, კომენტარები, ფოტოები)
- რეპორტების დამუშავება და ადმინისტრატორების ინფორმირება
- ტრანზაქციული ელფოსტები
- ყოველკვირეული digest ელფოსტა
- დაბადების დღის შეტყობინებები
- exception-ების შესახებ შეტყობინებები დეველოპერებისთვის

n8n გამოიყენება იმ პროცესებისთვის, რომლებიც backend-ის ძირითად ლოგიკას არ ეკუთვნის და რომელთა შეცვლაც API-ის ხელახალი deploy-ის გარეშე უნდა იყოს შესაძლებელი. backend მნიშვნელოვან მოვლენებს (რეგისტრაცია, პოსტის ან კომენტარის შექმნა, რეპორტი, exception) n8n-ის webhook-ებზე აგზავნის, საერთო საიდუმლო გასაღებით ხელმოწერილს. n8n კი backend-ს მიმართავს ცალკე სერვისული ანგარიშით (API key), რომელსაც წვდომა მხოლოდ შიდა და მოდერაციის endpoint-ებზე აქვს.

**კონტენტის AI მოდერაცია.** ახალი პოსტის, კომენტარის ან ფოტოს შექმნისას n8n ტექსტს და ფოტოებს AI მოდერაციის სერვისით ამოწმებს. აშკარად დამრღვევი კონტენტი ავტომატურად იმალება, საეჭვო კი ადმინისტრატორის განსახილველად მოინიშნება. ორივე შემთხვევაში ადმინისტრატორები იღებენ შეტყობინებას (Telegram, ელფოსტა).

**რეპორტების დამუშავება.** ყოველი ახალი რეპორტის შესახებ n8n ადმინისტრატორებს აცნობებს. თუ ერთსა და იმავე კონტენტზე მოკლე დროში რამდენიმე რეპორტი შემოვა, კონტენტი ავტომატურად იმალება განხილვამდე.

**ტრანზაქციული ელფოსტები.** მისალმების, პაროლის აღდგენის, ანგარიშის აღდგენისა და უსაფრთხოების შეტყობინებების ელფოსტებს n8n აგზავნის შაბლონების გამოყენებით. backend მხოლოდ მოვლენას და საჭირო მონაცემებს გადასცემს, ამიტომ ელფოსტის ტექსტისა და დიზაინის შეცვლა კოდის ცვლილებას არ საჭიროებს.

**ყოველკვირეული digest.** კვირაში ერთხელ n8n თითოეულ მომხმარებელს უგზავნის ელფოსტას წაუკითხავი შეტყობინებების, მეგობრობის მოთხოვნებისა და feed-ის პოპულარული პოსტების მოკლე მიმოხილვით. მომხმარებელს შეუძლია ამ ელფოსტაზე უარის თქმა პროფილის პარამეტრებში.

**დაბადების დღის შეტყობინებები.** ყოველდღიურად n8n პოულობს მომხმარებლებს, რომელთაც დაბადების დღე აქვთ, და მათ მეგობრებს აპლიკაციაში რეალურ დროში და ელფოსტით აცნობებს.

**exception-ების შესახებ შეტყობინებები.** სერვერზე წარმოქმნილი exception-ები და background სერვისების exception-ები n8n-ს გადაეცემა, რომელიც დეველოპერებს Telegram-ზე ან Discord-ზე მყისიერად აცნობებს. ერთნაირი exception-ების სერია ერთ შეტყობინებად ჯგუფდება.

### უსაფრთხოება და სისტემური ფუნქციები:

- მოთხოვნების რაოდენობის შეზღუდვა (rate limiting)
- exception-ების ცენტრალიზებული დამუშავება და ლოგირება
- background პროცესები (ძველი მონაცემების ავტომატური გასუფთავება)
- Swagger დოკუმენტაცია
- ინტეგრაციული ტესტები

სისტემა ზღუდავს მოთხოვნების რაოდენობას როგორც IP მისამართის (შესვლა, რეგისტრაცია, პაროლის აღდგენა), ისე მომხმარებლის მიხედვით, რაც იცავს პლატფორმას გარკვეული შეტევებისგან (DOS). თუ მოთხოვნის დამუშავებისას სერვერზე მოულოდნელი exception წარმოიქმნება, კლიენტი იღებს სტანდარტული ფორმატის პასუხს (ProblemDetails) შიდა დეტალების გამჟღავნების გარეშე, ხოლო exception-ის სრული ინფორმაცია იწერება ცალკე ლოგ ფაილში.

## API Endpoints

```
AUTH:
POST  /api/auth/register
POST  /api/auth/login
POST  /api/auth/refresh
PATCH /api/auth/logout
PUT   /api/auth/request-password-reset
PUT   /api/auth/reset-password

USERS:
POST /api/users
GET  /api/users/{id}
GET  /api/users/{username}
GET  /api/users/mine
GET  /api/users/search
PUT  /api/users/mine
PUT  /api/users/{id}

ACCOUNTS:
PATCH  /api/accounts/mine/deactivate
PATCH  /api/accounts/{id}/deactivate
PATCH  /api/accounts/{id}/activate
PATCH  /api/accounts/request-activate
PATCH  /api/accounts/activate
PUT    /api/accounts/{id}/assign-administrator
PUT    /api/accounts/{id}/remove-administrator
DELETE /api/accounts/mine
DELETE /api/accounts/{id}

POSTS:
GET    /api/posts/feed
GET    /api/posts/mine
GET    /api/posts/{id}
GET    /api/posts/user/{userId}
POST   /api/posts                          (multipart/form-data: ტექსტი + ფოტოები)
PUT    /api/posts/{id}
DELETE /api/posts/{id}
DELETE /api/posts/{postId}/images/{imageId}

LIKES:
GET    /api/posts/{postId}/likes
POST   /api/posts/{postId}/likes
DELETE /api/posts/{postId}/likes

COMMENTS:
GET    /api/comments/post/{postId}
GET    /api/comments/mine
GET    /api/comments/{id}
POST   /api/comments/posts/{postId}
PUT    /api/comments/{id}
DELETE /api/comments/{id}

FRIENDSHIPS:
GET    /api/friendships/accepted
GET    /api/friendships/pending
GET    /api/friendships/sent
GET    /api/friendships/{friendId}
GET    /api/friendships/accepted/{friendId}
GET    /api/friendships/user/{userId}
POST   /api/friendships/{addresseeId}
PUT    /api/friendships/{requesterId}/accept
PUT    /api/friendships/{requesterId}/decline
DELETE /api/friendships/{friendId}

MESSAGES:
GET    /api/messages/conversation/{otherUserId}
GET    /api/messages/friends/conversation
GET    /api/messages/friends/no-conversation
GET    /api/messages/conversations/unread
POST   /api/messages/{receiverId}          (multipart/form-data: ტექსტი + ფოტოები)
PUT    /api/messages/conversation/{otherUserId}/mark-as-read
PUT    /api/messages/{id}
DELETE /api/messages/{id}

REPORTS:
POST /api/reports
GET  /api/reports
PUT  /api/reports/{id}

ADMIN:
GET /api/admin/logs
GET /api/admin/statistics
GET /api/admin/moderation-queue
PUT /api/admin/posts/{id}/moderation
PUT /api/admin/comments/{id}/moderation

INTERNAL (n8n, API key):
GET  /api/internal/digest
GET  /api/internal/birthdays
POST /api/internal/notifications

SIGNALR HUB (/hubs/messages):
server → client: ReceiveMessage, MessageEdited, MessageSeen, ReceiveComment, PostLiked,
                 UserOnline, UserOffline, UserTyping, ReceiveNotification
client → server: SendTyping
```

## მონაცემთა მოდელები

```
User {
  id: number
  username: string
  email: string
  passwordHash: string
  passwordSalt: string
  bio: string
  dateOfBirth: date
  lastOnlineAt: datetime
  usernameLastChangedAt: datetime
  accountDeactivatedAt: datetime
  accountDeletedAt: datetime
  digestEmailEnabled: boolean
  createdAt: datetime
}

Role {
  id: number
  name: enum [USER, ADMINISTRATOR]
}

UserRole {
  userId: number
  roleId: number
}

Post {
  id: number
  userId: number
  postTitle: string
  postContent: text
  moderationStatus: enum [VISIBLE, FLAGGED, HIDDEN]
  createdAt: datetime
  lastUpdatedAt: datetime
}

PostImage {
  id: number
  postId: number
  imageUrl: string
  orderIndex: number
  createdAt: datetime
}

PostLike {
  id: number
  postId: number
  userId: number
  createdAt: datetime
}

Comment {
  id: number
  postId: number
  commenterUserId: number
  commentContent: text
  moderationStatus: enum [VISIBLE, FLAGGED, HIDDEN]
  createdAt: datetime
  lastUpdatedAt: datetime
}

Friendship {
  id: number
  requesterUserId: number
  addresseeUserId: number
  friendshipStatus: enum [PENDING, ACCEPTED, DECLINED]
  sentAt: datetime
  lastUpdatedAt: datetime
}

Message {
  id: number
  senderUserId: number
  receiverUserId: number
  messageContent: text
  seen: boolean
  seenAt: datetime
  createdAt: datetime
  lastUpdatedAt: datetime
}

MessageImage {
  id: number
  messageId: number
  imageUrl: string
  orderIndex: number
  createdAt: datetime
}

Report {
  id: number
  reporterUserId: number
  targetType: enum [POST, COMMENT, USER]
  targetId: number
  reason: text
  status: enum [PENDING, RESOLVED, REJECTED]
  reviewedByUserId: number
  createdAt: datetime
  reviewedAt: datetime
}

RefreshToken {
  id: number
  userId: number
  tokenHash: string
  expiresAt: datetime
  revokedAt: datetime
}

Log {
  id: number
  userId: number
  authorizedRequest: boolean
  succeeded: boolean
  action: string
  details: text
  entityName: string
  entityId: number
  createdAt: datetime
}
```
