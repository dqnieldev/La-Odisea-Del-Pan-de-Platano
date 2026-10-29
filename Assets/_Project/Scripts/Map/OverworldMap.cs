using System;
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

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int IsGroundedId = Animator.StringToHash("IsGrounded");

        private readonly List<Vector2> route = new();
        private int current, target = -1;
        private bool entering;
        private float stepTimer;
        private int lastHorizontal;

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
            ShowCurrent();
        }

        private void Update()
        {
            if (Walking)
                Walk();
            AnimateToken();
            if (entering || Walking || SceneFlow.IsLoading || Time.timeScale == 0f)
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
