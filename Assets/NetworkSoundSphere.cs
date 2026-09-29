using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Netzwerk-Erweiterung für SoundSphere.
/// 
/// SETUP auf dem Prefab:
/// 1. SoundSphere (dein bestehendes Script) — UNVERÄNDERT
/// 2. NetworkObject — neu
/// 3. ClientNetworkTransform — neu (Is Server Authoritative: FALSE)
/// 4. NetworkPhysicsInteractable — neu
/// 5. Rigidbody — neu
/// 6. Dieses Script (NetworkSoundSphere) — neu
/// </summary>
[RequireComponent(typeof(SoundSphere))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkSoundSphere : NetworkBehaviour
{
    private SoundSphere m_SoundSphere;

    // ── Netzwerk-Zustand ──────────────────────────────────────────
    // Wir überlassen dem Server die Verwaltung des An/Aus-Zustands.
    // So gibt es keine Konflikte, wenn zwei Spieler gleichzeitig drücken.
    private NetworkVariable<bool> m_NetIsActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        m_SoundSphere = GetComponent<SoundSphere>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 1. Initialen Zustand vom Server laden (falls man später beitritt)
        SyncStateLocally(false, m_NetIsActive.Value);

        // 2. Auf Netzwerk-Änderungen hören (Alle Clients)
        m_NetIsActive.OnValueChanged += SyncStateLocally;

        // 3. Auf lokale Aktionen (Knopfdruck / Greifen) vom SoundSphere-Script hören
        m_SoundSphere.OnToggleRequested += RequestToggle;
        m_SoundSphere.OnGrabRequested += RequestOwnership;
    }

    public override void OnNetworkDespawn()
    {
        m_NetIsActive.OnValueChanged -= SyncStateLocally;
        m_SoundSphere.OnToggleRequested -= RequestToggle;
        m_SoundSphere.OnGrabRequested -= RequestOwnership;
        
        base.OnNetworkDespawn();
    }

    // ── VON LOKAL ZUM NETZWERK (Spieler agiert) ───────────────────

    private void RequestToggle(bool newState)
    {
        // Wir senden den Wunsch, die Kugel an/aus zu schalten, an den Server
        ToggleStateServerRpc(newState);
    }

    private void RequestOwnership()
    {
        // Wenn wir die Kugel greifen, brauchen wir die "Ownership" (Besitzrechte).
        // Nur so darf unsere "ClientNetworkTransform" die Bewegung an die anderen senden!
        if (!IsOwner && NetworkManager.Singleton != null)
        {
            RequestOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    // ── SERVER LOGIK (Nur auf dem Server ausgeführt) ──────────────

    [ServerRpc(RequireOwnership = false)]
    private void ToggleStateServerRpc(bool newState)
    {
        // Server ändert den Wert -> OnValueChanged feuert auf ALLEN Clients
        m_NetIsActive.Value = newState;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ulong clientId)
    {
        // Server gibt diesem Spieler die Erlaubnis, die Kugel zu bewegen
        GetComponent<NetworkObject>().ChangeOwnership(clientId);
    }

    // ── VOM NETZWERK ZU LOKAL (Server hat Zustand geändert) ───────

    private void SyncStateLocally(bool previous, bool current)
    {
        if (current)
        {
            m_SoundSphere.ActivateFromNetwork();
        }
        else
        {
            m_SoundSphere.DeactivateFromNetwork();
        }
    }
}