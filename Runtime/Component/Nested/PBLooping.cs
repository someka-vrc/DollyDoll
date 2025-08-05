using UnityEngine;

namespace Somekasu.DollyDoll
{
    public enum PBLooping
    {
        ///<summary> No repeat </summary>
        [InspectorName("なし - None")]
        None,
        ///<summary> Repeat looping restarts the path from the start once the camera reaches the end </summary>
        [InspectorName("繰り返す - Repeat")]
        Repeat,
        ///<summary> Reverse changes direction whenever the camera reaches the start or end of the path </summary>
        [InspectorName("巻き戻す - Reverse")]
        Reverse,
        ///<summary> Revolve connects the end of a path back to the start so it loops smoothly </summary>
        [InspectorName("繰り返す(補間あり) - Revolve")]
        Revolve
    }
}