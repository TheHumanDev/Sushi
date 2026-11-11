# Sushi Issue Triage and Lifecycle

This document describes how maintainers classify, evaluate, prioritize, and manage issues in the Sushi programming language repository.

The goal is to maintain a transparent, consistent process while minimizing administrative overhead. These guidelines govern maintainer decisions; they do not impose additional requirements on contributors.

## 1. Reporting Issues

Contributors should use the appropriate GitHub Issue Form:

- **Bug Report:** Incorrect or unexpected behavior in Sushi or its tooling.
- **Documentation Issue:** Missing, inaccurate, unclear, or contradictory documentation.
- **Implementation Request:** Actionable development work within Sushi's established direction.

Substantial language design proposals and architectural changes should first be discussed through [GitHub Discussions](https://github.com/TheHumanDev/Sushi/discussions). Submitting an issue does not automatically approve the requested work.

Suspected security vulnerabilities must be reported privately according to the [Security Policy](SECURITY.md), not through public issues.

## 2. Initial Triage

Issue forms automatically assign an appropriate type label and `Status: Needs Triage`.

Maintainers then:

1. Review the report and available evidence.
2. Determine whether additional information is necessary.
3. Check for existing issues involving the same underlying problem.
4. Evaluate the issue's validity, scope, and relationship to accepted project requirements.
5. Assign or correct labels, including relevant area and priority labels.
6. Record the disposition and any relevant dependencies or references.

Maintainers may correct the initial type classification when the submitted form does not accurately reflect the issue.

Classification and acceptance remain maintainer responsibilities. Contributors may suggest classifications or priorities, but those suggestions are not authoritative.

## 3. Issue Types

Use the following labels to identify the nature of the work:

| Label | Meaning |
|---|---|
| `Type: Bug` | Incorrect or unexpected behavior |
| `Type: Documentation` | Documentation corrections and improvements |
| `Type: Implementation` | Actionable functionality or development work |
| `Type: Refactor` | Internal restructuring without intended behavior changes |
| `Type: Maintenance` | Dependencies, tooling, and routine upkeep |

An issue's type should describe its primary purpose. Maintainers may change the type as the issue becomes better understood.

## 4. Issue Status

Use the following labels to communicate the issue's current triage disposition:

| Label | Meaning |
|---|---|
| `Status: Needs Triage` | Awaiting initial maintainer evaluation |
| `Status: Needs Info` | Additional information is necessary |
| `Status: Accepted` | Recognized as valid, actionable work within the project's direction |
| `Status: Blocked` | Cannot proceed until an identified obstacle is resolved |

Use the status label that best describes the issue's current disposition.

**Acceptance does not imply scheduling, assignment, or a commitment to immediate implementation.**

A blocked issue should identify the obstacle or dependency where reasonably possible.

These labels do not replace GitHub's native open/closed states or development progress tracking through pull requests and GitHub Projects.

## 5. Priority

Maintainers assign priority according to the expected impact and urgency of the work.

| Label | Meaning |
|---|---|
| `Priority: Critical` | Requires immediate attention, such as widespread breakage or severe correctness concerns |
| `Priority: High` | Important work requiring timely attention |
| `Priority: Normal` | Standard development priority |
| `Priority: Low` | Minor improvements or reasonably deferrable work |

Priority is not necessarily equivalent to technical severity.

For example, an issue affecting a widely used compiler feature may deserve greater urgency than a severe defect in an obscure experimental feature.

New issues do not automatically receive a priority. Maintainers evaluate priority during triage and may revise it as circumstances change.

Security vulnerability severity is evaluated through the project's security process, separately from public issue priority labels.

## 6. Affected Areas

Area labels identify the subsystems involved:

| Label | Scope |
|---|---|
| `Area: Compiler` | Compiler pipeline and language implementation |
| `Area: Language Server` | Language Server Protocol functionality |
| `Area: VS Code` | Visual Studio Code extension |
| `Area: Build` | Build infrastructure, CI/CD, and toolchains |
| `Area: Standard Library` | Sushi's standard library |
| `Area: Documentation` | Language specification, guides, and references |
| `Area: Infrastructure` | Repository configuration and project infrastructure |

Multiple area labels may be assigned when an issue crosses subsystem boundaries.

More granular classifications may be introduced if issue volume or contributor needs justify them.

## 7. Contributor Discovery

Two conventional GitHub labels identify opportunities for external contributors:

- `good first issue`: Approachable, reasonably scoped work suitable for contributors becoming familiar with Sushi.
- `help wanted`: Accepted work for which external contributions are welcome.

These labels are assigned by maintainers. Neither guarantees that a contribution will be accepted without review.

## 8. Acceptance and Scope

Maintainers determine whether an issue represents valid work within Sushi's established direction.

Straightforward defects, documentation corrections, tests, and minor implementation tasks may be accepted without a separate design proposal.

Changes involving new language semantics, substantial architectural decisions, or fundamental safety and compatibility contracts require appropriate design discussion and acceptance before implementation.

An accepted issue may remain open without an assigned developer or scheduled milestone.

## 9. Duplicate Issues

Prefer one canonical issue for each confirmed underlying problem.

When a duplicate is identified:

1. Identify the issue that should remain authoritative.
2. Preserve unique reproduction steps, diagnostic information, environment details, or other relevant evidence.
3. Reference the canonical issue in a comment on the duplicate.
4. Mark the issue as a duplicate and close it using GitHub's native duplicate resolution.

Similar symptoms alone do not establish that two issues have the same underlying cause.

If reports involve distinct root causes, they should remain separate even when the visible behavior appears similar.

## 10. Regressions and Reopening

Reopen an existing issue when new evidence demonstrates that:

- Its original defect was not fully resolved.
- Its fix was incomplete.
- It was closed prematurely.

If a defect was demonstrably fixed and a later change reintroduces the behavior, create a new regression issue instead.

The new issue should link to the original and identify the affected versions or changes when known.

This distinction preserves an accurate record of the original resolution and subsequent regression.

## 11. Issue Closure

Use GitHub's native issue closure reasons rather than creating separate resolution labels.

| Reason | Appropriate use |
|---|---|
| **Completed** | The requested work has been completed or the defect has been verified as resolved |
| **Not Planned** | The request is rejected, invalid, obsolete, or outside the project's intended direction |
| **Duplicate** | The underlying problem is already tracked by a canonical issue |

Closures should be understandable to contributors.

When closing an issue as Not Planned, provide an explanation. When closing a duplicate, reference the canonical issue and preserve relevant information.

Do not mark unresolved work as completed merely because a related pull request was submitted or an attempted fix was implemented.

## 12. Inactivity and Long-Term Issues

Sushi does not automatically close issues because they have been inactive.

Legitimate defects, accepted improvements, and unresolved requirements may remain open indefinitely.

Inactivity does not by itself make an issue invalid, obsolete, or unimportant.

An issue awaiting additional information may retain `Status: Needs Info` when appropriate. Maintainers should make closure decisions based on substantive circumstances rather than an arbitrary inactivity period.

Stale-issue automation may be reconsidered if future project needs justify it.

## 13. Maintaining Issue Records

Maintainers should keep issue information accurate as understanding and priorities evolve.

When practical:

- Update labels when an issue's classification or disposition changes.
- Link relevant issues, pull requests, and design discussions.
- Record significant blockers or dependencies.
- Preserve useful findings when consolidating duplicate reports.
- Distinguish accepted but unscheduled work from active implementation.
- Prefer GitHub's native tracking capabilities over redundant workflow labels.

The purpose of issue tracking is to preserve useful technical knowledge, communicate decisions, and support development, not to create unnecessary administrative work.
