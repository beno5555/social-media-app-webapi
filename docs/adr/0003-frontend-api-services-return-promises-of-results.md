# Frontend API services return promises of results, not Observables

Angular's `HttpClient` returns Observables, and Angular code conventionally hands them on to components. WebCity's `…Api` services instead convert each call at the boundary and return `Promise<AppResult<T>>`: callers `await` it and branch on `succeeded`, and every expected failure (the backend's failure envelope, a validation error, a bodiless 401, 403 or 404, a network failure) arrives as the failure branch, never as a thrown error. This is the same shape as the backend's `async Task<ApplicationResponse<T>>`, so one set of control-flow and error rules covers both halves of the repository, and the owner reads `async/await` far more fluently than RxJS.

## Considered Options

- **Observables handed to components**: the Angular convention. Rejected, because every caller then needs RxJS operators and subscription handling, and the house control-flow rules (single exit, positive `if`, named conditions) do not translate to operator chains.
- **`httpResource` for reads**: stable since Angular 22. Rejected, because lists use infinite scroll, which appends pages, while a resource replaces its value on each request. It would also leave reads and writes in two different styles.

## Consequences

- Superseded requests are not cancelled automatically. Where that matters (user search), the caller ignores every response but the latest.
- RxJS appears only inside the one conversion helper, and wherever a library hands the app a stream.
- A forgotten `await` is a silent bug, so the lint rule against un-awaited promises is mandatory, not optional.
