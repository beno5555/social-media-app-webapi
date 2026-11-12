# UI text is translated in the frontend with typed dictionaries, and server text stays English

WebCity's UI has a language switch: English and Georgian first, Russian and Spanish to follow. The frontend translates only the text it owns (labels, buttons, its own validation and status messages), from one TypeScript dictionary file per language, each checked against the English one by the compiler. Text that comes from the backend (failure messages in the response envelope, validation messages, emails) stays English, because the envelope carries a message but no code to translate by.

## Considered Options

- **Angular's built-in i18n**: rejected. It produces one build per language and switches by reloading the page under another URL, which complicates the single-origin proxy setup (ADR 0001) and gives no in-place switch.
- **A runtime translation library (Transloco, ngx-translate)**: workable, and the upgrade path if needs grow (lazily loaded language files, richer message formats). Rejected for now, because the dictionaries need about fifty lines of our own code, the compiler proves every language complete, and there is no dependency to vet on Angular 22 without zone.js before the deadline.
- **Translating server text now**: deferred. It needs a stable code on every backend failure and localized email templates.

## Consequences

- A user of the Georgian UI sees server failure messages and emails in English. Forms validate in the browser first, so server validation text is rarely seen.
- No literal UI text may appear in a template or component.
- Never translate server text by matching its English wording; that breaks silently when the wording changes.
- Every language ships in the main bundle, which is acceptable at this size.
- The typeface must cover Latin, Cyrillic and Georgian.
