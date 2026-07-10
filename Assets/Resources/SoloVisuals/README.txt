Drop your own Chosen One art here to override the built-in placeholder visuals.

Create a prefab named after the SoloGameKind it's for:

  FieryFurnaceDash.prefab
  DavidsSlingshot.prefab
  JoyfulPrayer.prefab
  LoavesAndFishesMultiply.prefab
  PartingTheSea.prefab

Its root object needs a component implementing ISoloGameVisual (see
Assets/Scripts/Game/SoloGameVisuals.cs) - Setup/SetProgress/Teardown. Only
JoyfulPrayer and PartingTheSea currently have a built-in placeholder to fall
back to if no prefab exists; the other three still use GameManager's original
procedural UI-rectangle stage until you add a prefab (or wire up a fallback
for them the same way).

SoloGameVisualFactory.Create() checks here first
(Resources.Load<GameObject>("SoloVisuals/<KindName>")) - no code changes
needed once your prefab exists.
