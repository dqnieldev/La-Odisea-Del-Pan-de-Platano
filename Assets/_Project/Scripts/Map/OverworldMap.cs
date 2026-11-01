using System;
using System.Collections;
using System.Collections.Generic;
using Odisea.Core;
using Odisea.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.Map
{
    /// <summary>
    /// Mapa de San Blas como el de Mario: se ve completo y Ulises (una ficha) camina por los caminos dibujados entre
    /// los lugares. Clic en un lugar abierto: camina hasta él; clic otra vez (o Aceptar): entra. Con ← → (teclado,
    /// cruceta o stick) va al lugar abierto anterior o siguiente, en el orden de la historia. Los bloqueados solo
    /// avisan qué hay que terminar antes.
    /// Al pasar el mouse sobre un lugar, se ilumina y la ventanita lo muestra. Al volver con un lugar recién abierto,
    /// la cámara se acerca, le vuelve el color con un estallido de chispas y regresa. Sobre el lugar elegido rebota una flecha y brilla un
    /// anillo; su nombre sale en la ventanita de abajo.
    /// </summary>
    public class OverworldMap : MonoBehaviour
    {
        [Serializable]
        public class Node
        {
            public Vector2 position;
            [Tooltip("Lugar de este punto; vacío en los cruces de caminos.")]
            public MapLocation location;
        }

        [Serializable]
        public class Edge
        {
            public int from, to;
            [Tooltip("Puntos intermedios, de from a to, siguiendo el camino dibujado.")]
            public Vector2[] points = Array.Empty<Vector2>();
        }

        [SerializeField] private Node[] nodes = Array.Empty<Node>();
        [SerializeField] private Edge[] edges = Array.Empty<Edge>();
        [Tooltip("Nodos con lugar, en el orden de la historia (← → recorre esta lista).")]
        [SerializeField] private int[] locationOrder = Array.Empty<int>();

        [Header("Ficha de Ulises")]
        [SerializeField] private Transform token;
        [Tooltip("El dibujo (hijo): se voltea según hacia dónde camina.")]
        [SerializeField] private Transform tokenVisual;
        [Tooltip("Sin cuadros de la ficha: las animaciones de Ulises de los niveles.")]
        [SerializeField] private Animator tokenAnimator;
        [Tooltip("Ulises chibi del mapa: sus cuadros de reposo y de caminar (si hay, se usan en vez del Animator).")]
        [SerializeField] private SpriteRenderer tokenRenderer;
        [SerializeField] private Sprite[] idleFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] walkFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] private float idleFps = 2.5f;
        [SerializeField, Min(0.1f)] private float walkFps = 9f;
        [SerializeField, Min(0.1f)] private float speed = 9f;
        [SerializeField] private LocationPanel panel;
        [SerializeField] private SoundEffect footstep = new();
        [SerializeField, Min(0.05f)] private float footstepInterval = 0.28f;
        [Tooltip("Cuánto brinca la ficha al caminar (unidades).")]
        [SerializeField, Min(0f)] private float walkBounce = 0.22f;

        [Header("Efectos")]
        [Tooltip("Polvito de los pasos (opcional).")]
        [SerializeField] private MapAmbience ambience;
        [SerializeField] private Color dustColor = new(0.85f, 0.72f, 0.52f, 0.8f);
        [Tooltip("Flecha que rebota sobre el letrero del lugar elegido.")]
        [SerializeField] private Transform marker;
        [Tooltip("Anillo que brilla en el claro del lugar elegido.")]
        [SerializeField] private SpriteRenderer highlight;
        [Tooltip("Anillo del lugar bajo el mouse (si no es donde está Ulises).")]
        [SerializeField] private SpriteRenderer hoverRing;
        [SerializeField] private SoundEffect unlockSound = new();
        [SerializeField] private Color burstColor = new(1f, 0.85f, 0.35f, 1f);
        [Tooltip("Al estrenar un lugar, el tamaño de la cámara respecto al mapa completo.")]
        [SerializeField, Range(0.2f, 1f)] private float revealZoom = 0.55f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int IsGroundedId = Animator.StringToHash("IsGrounded");

        private readonly List<Vector2> route = new();
        private int current, target = -1;
        private bool entering, revealing;
        private float stepTimer;
        private int lastHorizontal;
        private Vector3 visualHome, markerScale = Vector3.one;
        private Color highlightColor = Color.white, hoverColor = Color.white;
        private float focus, hoverFocus;
        private int hovered = -1;
        private Camera view;
        private Vector3 viewHome;
        private float viewSize;

        private bool Walking => route.Count > 0;

        private void Start()
        {
            current = locationOrder.Length > 0 ? locationOrder[0] : 0;
            string last = GameProgress.LastMapLocation;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].location != null && nodes[i].location.Id == last)
                    current = i;
            }
            token.position = nodes[current].position;
            if (tokenAnimator != null)
                tokenAnimator.SetBool(IsGroundedId, true);
            if (tokenVisual != null)
                visualHome = tokenVisual.localPosition;
            if (marker != null)
                markerScale = marker.localScale;
            if (highlight != null)
                highlightColor = highlight.color;
            if (hoverRing != null)
                hoverColor = hoverRing.color;
            view = Camera.main;
            if (view != null)
            {
                viewHome = view.transform.position;
                viewSize = view.orthographicSize;
            }
            ShowCurrent();
            StartCoroutine(RevealPlaces());
        }

        private void Update()
        {
            if (Walking)
                Walk();
            AnimateToken();
            AnimateFocus();
            bool busy = entering || revealing || Walking || SceneFlow.IsLoading || Time.timeScale == 0f;
            UpdateHover(busy);
            if (busy)
                return;

            if (ClickedLocation(out int clicked))
            {
                if (clicked == current)
                    TryEnter();
                else
                    GoTo(clicked);
                return;
            }
            int step = HorizontalPressed();
            if (step != 0)
                Step(step);
            else if (ConfirmPressed())
                TryEnter();
        }

        private void AnimateToken()
        {
            Sprite[] frames = Walking ? walkFrames : idleFrames;
            if (tokenRenderer != null && frames.Length > 0)
            {
                float fps = Walking ? walkFps : idleFps;
                tokenRenderer.sprite = frames[(int)(Time.unscaledTime * fps) % frames.Length];
            }
            else if (tokenAnimator != null)
            {
                tokenAnimator.SetFloat(SpeedId, Walking ? speed : 0f);
            }
            // Brinquitos al caminar (uno por paso).
            if (tokenVisual != null)
            {
                float hop = Walking ? Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI / footstepInterval)) * walkBounce : 0f;
                Vector3 position = tokenVisual.localPosition;
                position.y = Mathf.MoveTowards(position.y, visualHome.y + hop, Time.deltaTime * 4f);
                tokenVisual.localPosition = position;
            }
        }

        // Flecha y anillo del lugar donde está Ulises. Se apagan mientras camina o mientras se estrena un lugar.
        private void AnimateFocus()
        {
            MapLocation location = nodes[current].location;
            focus = Mathf.MoveTowards(focus, Walking || revealing || location == null ? 0f : 1f, Time.deltaTime * 4f);
            if (location == null)
                return;

            if (marker != null)
            {
                float bounce = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.45f;
                marker.position = (Vector3)nodes[current].position + Vector3.up * (MarkerHeight + bounce);
                marker.localScale = markerScale * focus;
            }
            if (highlight != null)
            {
                highlight.transform.position = nodes[current].position;
                float pulse = 0.75f + Mathf.Sin(Time.time * 3f) * 0.25f;
                highlight.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a * pulse * focus);
                float size = HighlightSize / Mathf.Max(0.01f, highlight.sprite.bounds.size.x);
                highlight.transform.localScale = Vector3.one * size * (1f + Mathf.Sin(Time.time * 3f) * 0.03f);
            }
        }

        // Altura de la flecha sobre la base del lugar: un poco arriba de la cabeza de Ulises.
        private const float MarkerHeight = 4.9f;

        // Ancho del anillo en el mundo: un poco más que la base de piedra de un lugar.
        private const float HighlightSize = 4.6f;

        // ---------- Mouse encima ----------

        // El lugar bajo el mouse (que no sea donde está Ulises) se ilumina con un anillo y la ventanita lo muestra.
        private void UpdateHover(bool busy)
        {
            int now = busy ? -1 : LocationUnderMouse();
            if (now == current)
                now = -1;
            if (now != hovered)
            {
                if (now >= 0)
                {
                    panel.Preview(nodes[now].location);
                    UiAudio.Play(UiSound.Hover);
                }
                else if (!busy)
                {
                    panel.EndPreview();
                }
                hovered = now;
            }
            hoverFocus = Mathf.MoveTowards(hoverFocus, hovered >= 0 ? 1f : 0f, Time.deltaTime * 6f);
            if (hoverRing == null)
                return;
            if (hovered >= 0)
                hoverRing.transform.position = nodes[hovered].position;
            float size = HighlightSize / Mathf.Max(0.01f, hoverRing.sprite.bounds.size.x);
            hoverRing.transform.localScale = Vector3.one * size * (0.8f + 0.2f * hoverFocus);
            hoverRing.color = new Color(hoverColor.r, hoverColor.g, hoverColor.b, hoverColor.a * hoverFocus);
        }

        private int LocationUnderMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || view == null)
                return -1;
            Vector2 point = view.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(point);
            MapLocation location = hit != null ? hit.GetComponentInParent<MapLocation>() : null;
            if (location == null)
                return -1;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].location == location)
                    return i;
            }
            return -1;
        }

        // ---------- Lugares nuevos ----------

        // Los lugares abiertos en color (los cerrados, en blanco y negro). Los recién abiertos se estrenan en orden: les
        // vuelve el color. Mientras tanto no se puede elegir lugar.
        private IEnumerator RevealPlaces()
        {
            int open = 0;
            for (int i = 0; i < locationOrder.Length; i++)
            {
                if (!nodes[locationOrder[i]].location.IsLocked)
                    open = i + 1;
            }
            int seen = GameProgress.MapRevealed;
            for (int i = 0; i < locationOrder.Length; i++)
                nodes[locationOrder[i]].location.ShowColor(i < Mathf.Min(seen, open));
            if (seen >= open)
            {
                GameProgress.MapRevealed = Mathf.Max(seen, open);
                yield break;
            }

            revealing = true;
            yield return new WaitForSeconds(0.6f);
            for (int index = seen; index < open; index++)
            {
                // La cámara se acerca al lugar, le vuelve el color con un estallido de chispas y regresa.
                MapLocation location = nodes[locationOrder[index]].location;
                Vector2 spot = nodes[locationOrder[index]].position;
                panel.Preview(location, "¡Nuevo lugar!");
                yield return MoveCamera(spot + Vector2.up * 2f, viewSize * revealZoom, 0.7f);
                unlockSound.Play();
                if (ambience != null)
                    ambience.Burst(spot, burstColor);
                yield return location.RevealColor();
                // Se guarda en cuanto se estrena cada uno, por si sale del mapa a la mitad.
                GameProgress.MapRevealed = index + 1;
                yield return new WaitForSeconds(0.45f);
            }
            yield return MoveCamera(viewHome, viewSize, 0.6f);
            panel.EndPreview();
            ShowCurrent();
            revealing = false;
        }

        // Lleva la cámara a un punto y tamaño (sin salirse del mapa), con entrada y salida suaves.
        private IEnumerator MoveCamera(Vector2 center, float size, float duration)
        {
            if (view == null)
                yield break;
            float aspect = view.aspect;
            float halfWidth = viewSize * aspect, halfHeight = viewSize;
            center.x = Mathf.Clamp(center.x, viewHome.x - halfWidth + size * aspect, viewHome.x + halfWidth - size * aspect);
            center.y = Mathf.Clamp(center.y, viewHome.y - halfHeight + size, viewHome.y + halfHeight - size);
            Vector3 fromPosition = view.transform.position;
            float fromSize = view.orthographicSize;
            var toPosition = new Vector3(center.x, center.y, fromPosition.z);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                view.transform.position = Vector3.Lerp(fromPosition, toPosition, p);
                view.orthographicSize = Mathf.Lerp(fromSize, size, p);
                yield return null;
            }
            view.transform.position = toPosition;
            view.orthographicSize = size;
        }

        // ← →: al lugar abierto anterior o siguiente en el orden de la historia (los bloqueados se saltan).
        private void Step(int direction)
        {
            int index = Array.IndexOf(locationOrder, current);
            for (int i = index + direction; i >= 0 && i < locationOrder.Length; i += direction)
            {
                MapLocation location = nodes[locationOrder[i]].location;
                if (!location.IsLocked)
                {
                    GoTo(locationOrder[i]);
                    return;
                }
            }
        }

        private void GoTo(int node)
        {
            MapLocation location = nodes[node].location;
            if (location != null && location.IsLocked)
            {
                panel.ShowLocked(location);
                return;
            }
            List<int> path = FindPath(current, node);
            if (path == null)
                return;
            route.Clear();
            for (int i = 0; i + 1 < path.Count; i++)
                AppendEdge(path[i], path[i + 1]);
            target = node;
            panel.Hide();
            UiAudio.Play(UiSound.Click);
        }

        private void Walk()
        {
            Vector2 position = token.position;
            float travel = speed * Time.deltaTime;
            while (route.Count > 0 && travel > 0f)
            {
                Vector2 next = route[0];
                Vector2 delta = next - position;
                if (Mathf.Abs(delta.x) > 0.01f && tokenVisual != null)
                {
                    Vector3 scale = tokenVisual.localScale;
                    scale.x = Mathf.Abs(scale.x) * Mathf.Sign(delta.x);
                    tokenVisual.localScale = scale;
                }
                float distance = delta.magnitude;
                if (distance <= travel)
                {
                    position = next;
                    travel -= distance;
                    route.RemoveAt(0);
                }
                else
                {
                    position += delta / distance * travel;
                    travel = 0f;
                }
            }
            token.position = position;

            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                footstep.Play();
                if (ambience != null)
                    ambience.Puff(token.position, dustColor);
                stepTimer = footstepInterval;
            }
            if (route.Count == 0)
            {
                current = target;
                ShowCurrent();
            }
        }

        private void ShowCurrent()
        {
            MapLocation location = nodes[current].location;
            if (location != null)
                panel.Show(location);
        }

        private void TryEnter()
        {
            MapLocation location = nodes[current].location;
            if (location == null || location.State != LocationState.Available)
                return;
            entering = true;
            UiAudio.Play(UiSound.Click);
            Vector2? center = null;
            Camera view = Camera.main;
            if (view != null)
            {
                Vector3 point = view.WorldToViewportPoint(token.position + Vector3.up * 0.8f);
                center = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
            }
            location.Enter(center);
        }

        // ---------- Caminos ----------

        // Camino más corto en número de tramos (el mapa es chico).
        private List<int> FindPath(int start, int goal)
        {
            var previous = new Dictionary<int, int> { [start] = -1 };
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                if (node == goal)
                    break;
                foreach (Edge edge in edges)
                {
                    int other = edge.from == node ? edge.to : edge.to == node ? edge.from : -1;
                    if (other < 0 || previous.ContainsKey(other))
                        continue;
                    previous[other] = node;
                    queue.Enqueue(other);
                }
            }
            if (!previous.ContainsKey(goal))
                return null;
            var path = new List<int>();
            for (int node = goal; node >= 0; node = previous[node])
                path.Insert(0, node);
            return path;
        }

        // Puntos del tramo a→b (al revés si el tramo está guardado de b a a) y el nodo b al final.
        private void AppendEdge(int a, int b)
        {
            foreach (Edge edge in edges)
            {
                if (edge.from == a && edge.to == b)
                {
                    route.AddRange(edge.points);
                    break;
                }
                if (edge.from == b && edge.to == a)
                {
                    for (int i = edge.points.Length - 1; i >= 0; i--)
                        route.Add(edge.points[i]);
                    break;
                }
            }
            route.Add(nodes[b].position);
        }

        // ---------- Entrada ----------

        private bool ClickedLocation(out int node)
        {
            node = -1;
            Mouse mouse = Mouse.current;
            Camera view = Camera.main;
            if (mouse == null || view == null || !mouse.leftButton.wasPressedThisFrame)
                return false;
            Vector2 point = view.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(point);
            MapLocation location = hit != null ? hit.GetComponentInParent<MapLocation>() : null;
            if (location == null)
                return false;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].location == location)
                {
                    node = i;
                    return true;
                }
            }
            return false;
        }

        // -1 / 1 solo en el cuadro en que se empuja (el stick no repite mientras se mantiene).
        private int HorizontalPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    return -1;
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    return 1;
            }
            int horizontal = 0;
            if (gamepad != null)
            {
                float x = gamepad.leftStick.x.ReadValue() + gamepad.dpad.x.ReadValue();
                horizontal = x > 0.5f ? 1 : x < -0.5f ? -1 : 0;
            }
            int pressed = horizontal != lastHorizontal ? horizontal : 0;
            lastHorizontal = horizontal;
            return pressed;
        }

        private static bool ConfirmPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame)) ||
                   (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        }
    }
}
