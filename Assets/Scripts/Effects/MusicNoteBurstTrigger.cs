using UnityEngine;
using UnityEngine.Events;

namespace MusicFX
{
    [RequireComponent(typeof(MusicNoteBurst))]
    [AddComponentMenu("Effects/Music Note Burst Trigger")]
    public class MusicNoteBurstTrigger : MonoBehaviour
    {
        public enum TriggerMode { Manual, KeyDown, OnTriggerEnter2D, OnCollisionEnter2D }

        [SerializeField] private TriggerMode mode = TriggerMode.KeyDown;
        [SerializeField] private KeyCode key = KeyCode.Space;
        [SerializeField] private string requiredTag = "";
        [SerializeField] private float cooldownSeconds = 0.05f;
        public UnityEvent onBurst;

        private MusicNoteBurst burst;
        private float lastTime = -999f;

        private void Awake() { burst = GetComponent<MusicNoteBurst>(); }

        private void Update()
        {
            if (mode == TriggerMode.KeyDown && Input.GetKeyDown(key)) Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (mode != TriggerMode.OnTriggerEnter2D) return;
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
            Fire();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (mode != TriggerMode.OnCollisionEnter2D) return;
            if (!string.IsNullOrEmpty(requiredTag) && !collision.collider.CompareTag(requiredTag)) return;
            Fire();
        }

        public void Fire()
        {
            if (Time.time - lastTime < cooldownSeconds) return;
            lastTime = Time.time;
            burst.Play();
            onBurst?.Invoke();
        }
    }
}
