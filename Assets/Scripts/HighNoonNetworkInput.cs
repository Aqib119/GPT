using Fusion;
using UnityEngine;

public struct HighNoonNetworkInput : INetworkInput
{
    public Vector2 Move;
    public float LookDelta;
    public NetworkBool Fire;
    public NetworkBool UseGyro;
}
