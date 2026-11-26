using Unity.Netcode;
using UnityEngine;

namespace FamousGreatGame
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class ArenaPlayer : NetworkBehaviour
    {
        public const float ArenaHalfSize = 10f;
        private const float Speed = 5f;
        private const float SendInterval = 0.05f;
        private const float InputTimeout = 0.35f;

        private Vector2 serverInput;
        private float lastInputAt;
        private float nextSendAt;
        internal Vector2? SmokeInput { get; set; }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                float angle = (OwnerClientId % 8) * (Mathf.PI / 4f);
                transform.position = new Vector3(Mathf.Cos(angle) * 3f, 0.8f, Mathf.Sin(angle) * 3f);
            }

            GetComponent<Renderer>().material.color = IsOwner
                ? new Color(0.20f, 0.82f, 0.94f)
                : new Color(1f, 0.50f, 0.22f);
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || Time.unscaledTime < nextSendAt)
                return;

            nextSendAt = Time.unscaledTime + SendInterval;
            var input = SmokeInput ?? new Vector2(
                Axis(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow),
                Axis(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow));

            if (IsServer)
                RecordInput(input);
            else
                SubmitInputRpc(input);
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || Time.unscaledTime - lastInputAt > InputTimeout)
                return;

            Vector3 position = transform.position;
            position += new Vector3(serverInput.x, 0f, serverInput.y) * (Speed * Time.fixedDeltaTime);
            position.x = Mathf.Clamp(position.x, -ArenaHalfSize + 0.5f, ArenaHalfSize - 0.5f);
            position.z = Mathf.Clamp(position.z, -ArenaHalfSize + 0.5f, ArenaHalfSize - 0.5f);
            transform.position = position;
        }

        [Rpc(SendTo.Server, RequireOwnership = true, Delivery = RpcDelivery.Unreliable)]
        private void SubmitInputRpc(Vector2 input) => RecordInput(input);

        private void RecordInput(Vector2 input)
        {
            if (!float.IsFinite(input.x) || !float.IsFinite(input.y))
                return;

            input.x = Mathf.Clamp(input.x, -1f, 1f);
            input.y = Mathf.Clamp(input.y, -1f, 1f);
            serverInput = Vector2.ClampMagnitude(input, 1f);
            lastInputAt = Time.unscaledTime;
        }

        private static int Axis(KeyCode positive, KeyCode positiveAlt, KeyCode negative, KeyCode negativeAlt)
        {
            return (Input.GetKey(positive) || Input.GetKey(positiveAlt) ? 1 : 0)
                - (Input.GetKey(negative) || Input.GetKey(negativeAlt) ? 1 : 0);
        }
    }
}
