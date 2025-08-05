using UnityEngine;

namespace Somekasu.DollyDoll
{
    public enum PBPathType
    {
        ///<summary> Fitted paths use B-Spline interpolation and are very smooth, but may not pass through points </summary>
        [InspectorName("フィット - Fitted")]
        Fitted,
        ///<summary> Loose paths use Catmull Rom interpolation and pass through the points you’ve added, while being slightly less smooth than Fitted paths </summary>
        [InspectorName("なめらか - Loose")]
        Loose,
        ///<summary> Linear paths are not smoothed </summary>
        [InspectorName("リニア - Linear")]
        Linear
    }
}