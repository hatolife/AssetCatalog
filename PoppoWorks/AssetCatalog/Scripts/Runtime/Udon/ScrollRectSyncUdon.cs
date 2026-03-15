// SPDX-License-Identifier: CC0-1.0

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace AssetCatalog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ScrollRectSyncUdon : UdonSharpBehaviour
    {
        public ScrollRect targetScrollRect;
        public float syncIntervalSeconds = 0.1f;
        public float syncThreshold = 0.002f;

        [UdonSynced] private float _syncedVerticalNormalizedPosition = 1f;

        private float _lastObservedVerticalNormalizedPosition = 1f;
        private float _lastSyncTime = -999f;
        private bool _initialized;

        private void Start()
        {
            if (targetScrollRect == null) return;

            _lastObservedVerticalNormalizedPosition = targetScrollRect.verticalNormalizedPosition;
            _initialized = true;

            if (Networking.IsOwner(gameObject))
            {
                _syncedVerticalNormalizedPosition = _lastObservedVerticalNormalizedPosition;
            }
            else
            {
                ApplySyncedPosition();
            }
        }

        private void Update()
        {
            if (targetScrollRect == null) return;

            float current = targetScrollRect.verticalNormalizedPosition;
            if (!_initialized)
            {
                _lastObservedVerticalNormalizedPosition = current;
                _initialized = true;
                return;
            }

            bool changed = Mathf.Abs(current - _lastObservedVerticalNormalizedPosition) >= syncThreshold;
            _lastObservedVerticalNormalizedPosition = current;
            if (!changed) return;

            if (!Networking.IsOwner(gameObject))
            {
                var localPlayer = Networking.LocalPlayer;
                if (localPlayer == null) return;
                Networking.SetOwner(localPlayer, gameObject);
            }

            float now = Time.time;
            if (now - _lastSyncTime < syncIntervalSeconds &&
                Mathf.Abs(current - _syncedVerticalNormalizedPosition) < syncThreshold)
            {
                return;
            }

            _syncedVerticalNormalizedPosition = current;
            _lastSyncTime = now;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            ApplySyncedPosition();
        }

        private void ApplySyncedPosition()
        {
            if (targetScrollRect == null) return;

            float clamped = Mathf.Clamp01(_syncedVerticalNormalizedPosition);
            targetScrollRect.verticalNormalizedPosition = clamped;
            _lastObservedVerticalNormalizedPosition = clamped;
        }
    }
}
