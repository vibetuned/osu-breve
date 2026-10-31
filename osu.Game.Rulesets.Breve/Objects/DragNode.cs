namespace osu.Game.Rulesets.Breve.Objects
{
    /// <summary>
    /// One node of a drag chain. It is hit by holding its lane key, or holding a tap over it, as the scan line passes.
    /// On the keyboard, a chain that moves across lanes is played as a glissando.
    /// </summary>
    public class DragNode : BreveHitObject
    {
        /// <summary>
        /// How large a node is compared to a tap.
        /// </summary>
        public const float NODE_PROPORTION = 0.375f;

        /// <summary>
        /// How large the first node of a chain is compared to a tap.
        /// </summary>
        public const float HEAD_PROPORTION = 0.625f;

        /// <summary>
        /// The previous node in the chain, which this node is linked to. Null for the head of the chain.
        /// </summary>
        public DragNode? PreviousNode { get; set; }

        public bool IsChainHead => PreviousNode == null;

        public override float Diameter => base.Diameter * (IsChainHead ? HEAD_PROPORTION : NODE_PROPORTION);
    }
}
