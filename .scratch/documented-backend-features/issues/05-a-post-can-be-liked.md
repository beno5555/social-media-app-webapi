# 05: A post can be liked

**What to build:** A user likes a post and takes the like back. The author is told at once over the hub. Every post a reader gets shows how many likes and comments it has and whether the reader liked it, and anyone who can read the post can page through the users who liked it. None of this exists today.

- **`POST /api/posts/{postId}/likes`** likes, **`DELETE`** unlikes, **`GET`** lists the users who liked, in the minimal user shape.
- One like per user per post. A second like succeeds and changes nothing (owner's decision, 2026-11-14); an unlike of a post one has not liked is refused with a message.
- **`PostLiked`** goes to the author's group only, and not when the author likes their own post. Its payload names the post, the user who liked it and the new like count.
- The standard and full post shapes gain a like count, a comment count and a "liked by caller" flag. The flag is false for a guest. On the reads that guests may also call, a signed-in caller still gets their real flag.
- Counts come from the same query as the posts, not from a query per post.

SQL Server refuses two cascading paths to one table. A like points at a post and at a user, and a post also points at a user, so one of those cannot cascade. The purge of soft-deleted users must delete a purged user's likes explicitly where the database cannot, as it already does for messages, friendships and posts.

This ticket is listed after ticket 02 because both change how posts are read and returned; doing them at the same time would produce conflicting changes. Likes do not need photos.

Decisions are under "Likes" in `.scratch/documented-backend-features/spec.md`.

**Blocked by:** 01 (Real-time tests connect a real SignalR client), 02 (A post can be created with photos)

**Status:** done

**Difficulty:** tricky: a live push with a precise target, and counts added to every post read without a query per post.

- [x] A user likes a post; a second like by the same user succeeds and adds no like (changed from "is refused" by the owner, 2026-11-14)
- [x] A user unlikes a post they liked; unliking a post they have not liked is refused
- [x] Liking, unliking or listing likes of an unknown post is not found
- [x] The list of users who liked a post is paged and readable by a guest
- [x] Every post read returns the post's like count and comment count
- [x] "Liked by caller" is true for a signed-in caller who liked the post, including on reads a guest may call, and false for a guest
- [x] Post lists get their counts without a query per post
- [x] A like pushes `PostLiked` to the author's connected client with the post, the user who liked and the new count, and to no other connected user
- [x] Liking one's own post pushes nothing
- [x] Deleting a post deletes its likes, and the purge of soft-deleted users deletes the likes those users gave
- [x] Seed data gains likes without changing any existing seeded id
- [x] Each new action has its own rate-limit policy
- [x] `docs/backend/controllers.md`, `docs/backend/data.md`, `docs/backend/signalr.md` and `README.md` describe likes and the `PostLiked` event
