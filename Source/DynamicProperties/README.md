# Dynamic Property Management

This is a developer-facing API that fully reimplements how the stock code handles
`MaterialPropertyBlock`s (MPBs).

## Public API

TODO.

## Implementation Notes

The `MaterialPropertyManager` class implements very little behavior. It maintains an association of
registered `Renderer`s to their `Props` instances, stored in `PropsCascade` instances that
facilitate the sorting of the `Props` by priority.

Upon the addition or removal of a `Props` instance from a `Cascade`, it queries the
`MpbCompilerCache` singleton for a `MpbCompiler` instance linked to the same cascade (lowercase,
_i.e._ a sorted set of `Props`). The renderer managed by the `Cascade` is unregistered from the
previous `Compiler` instance, and registered to the new instance. The previous `Compiler` instance
then checks if it has any remaining linked renderers, and evicts itself from the cache if it has
become unused.

The `MpbCompiler` maintains a "manager map" of all the property IDs to their managing `Props`
instances, resolved by priority in case of conflict, as well as a single Unity MPB applied to all of
its linked renderers. Upon creation, the `Compiler` registers change handlers to each of its `Props`
instances. There are two types of handlers. When an existing property is changed, the value-changed
handler is fired. This is a fast path, as the managing `Props` of a given ID cannot change. Only
that particular entry of the MPB is updated, and is applied immediately to all linked renderers.
Upon the addition or removal of a property from a `Props`, the entry-changed handler is fired, to
recompute the manager map. The MPB is cleared, repopulated, and reapplied.

The mod-facing `Props` handles are stored throughout the stack and must be explicitly `Dispose`d.
Upon disposal, `MaterialPropertyManager` removes it from all registered `Cascade`s. This unlinks
each `Cascade` from their `MpbCompiler`s. As all such compilers will reference the disposed `Props`,
they will become dead themselves and be disposed upon unregistration of the last `Cascade`.

If a renderer is detected to be Unity GCed during MPB application by a `Compiler`, it is removed
from the `MaterialPropertyManager` using the public API. This disposes the associated `Cascade`, and
would dispose the originating `Compiler` instance if its last renderer was unregistered.

Upon destruction at scene change, `MaterialPropertyManager` disposes all `Cascade`s. This should
clear all `Compiler` cache instances, and is checked to have done so. Any remaining `Props`
instances would be kept alive by external references, and would have been unlinked from their update
handlers.
