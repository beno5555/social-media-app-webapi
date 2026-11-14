# 08: The feed ranks friends' and other users' posts

**What to build:** The feed shows posts by the caller's friends and by other users, ordered by a score instead of by date. Today it shows friends' posts only, newest first, so a new user with no friends sees an empty feed and nobody discovers anyone.

The score rises with the caller's connection to the author and with the post's activity, and falls with the post's age:

- **Connection** has three tiers: a friend, a user who shares at least one friend with the caller, anyone else.
- **Activity** is likes plus comments.
- **Age** lowers the score as time passes.

The weights and the decay are named constants. The caller's own posts are left out.

Ranking and paging happen in one database query; the feed never loads all posts into memory. Raw SQL is acceptable if the expression does not translate, as the conversation list already does. Ties are broken by newest id so the order is deterministic. Paging stays by page number; a post repeating or being skipped between two pages loaded minutes apart is accepted.

The tests assert the ordering properties, not the formula, using seed posts with fixed creation times.

Decisions are under "Feed ranking" in `.scratch/documented-backend-features/spec.md`. Hidden content is ticket 09.

**Blocked by:** 05 (A post can be liked)

**Status:** done

**Difficulty:** tricky: the score and the mutual-friends tier must be computed and paged inside one database query, and the weights need judgment.

- [x] The feed contains posts by friends and by users who are not friends
- [x] With equal age and activity, a friend's post ranks above a post by a user with a mutual friend, which ranks above a stranger's post
- [x] With equal connection and age, the post with more likes and comments ranks first
- [x] With equal connection and activity, the newer post ranks first
- [x] A user with no friends gets a non-empty feed
- [x] The caller's own posts never appear
- [x] Posts by deactivated or soft-deleted accounts do not appear
- [x] Two posts with the same score come back in the same order on every request
- [x] Ranking and paging run in one database query, with no in-memory sort over all posts
- [x] The weights and the decay are named constants
- [x] Seed data gains posts with fixed creation times without changing any existing seeded id
- [x] `docs/backend/controllers.md` and `README.md` describe the ranked feed
