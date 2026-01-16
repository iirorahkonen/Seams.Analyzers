---
name: Feature Request
about: Suggest a new analyzer rule or enhancement
title: '[FEATURE] '
labels: enhancement
assignees: ''
---

## Summary

A clear and concise description of what you want.

## Problem

What testability problem does this address? What seam-blocking pattern does it detect?

## Code Example

```csharp
// Example of problematic code this would detect
public class Example
{
    // Code that blocks seams...
}
```

## Suggested Solution

```csharp
// Example of the preferred pattern
public class Example
{
    // Code with proper seams...
}
```

## Category

Which category would this rule belong to?

- [ ] Direct Dependencies (SEAM001-003)
- [ ] Static Dependencies (SEAM004-008)
- [ ] Inheritance Blockers (SEAM009-011)
- [ ] Global State (SEAM012-014)
- [ ] Infrastructure Dependencies (SEAM015-018)
- [ ] New category

## Additional Context

References to "Working Effectively with Legacy Code" or other sources that support this pattern.
