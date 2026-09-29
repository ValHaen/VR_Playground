using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class LocalIPDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text m_IPText;

    void Start()
    {
        m_IPText.text = GetLocalIP();
    }

    string GetLocalIP()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "IP nicht gefunden";
        }
        catch
        {
            return "Fehler beim Laden der IP";
        }
    }
}