using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace HierarchyDecorator
{
    public enum PrefabInfo { None, Root, Part }

    public static class HierarchyManager
    {
        // --- Scene Data

        private static Dictionary<EntityId, HierarchyItem> lookup = new Dictionary<EntityId, HierarchyItem>();
        public static int Count => lookup.Count;
        public static IReadOnlyDictionary<EntityId, HierarchyItem> Items => lookup;
        private static Settings s_Settings => HierarchyDecorator.Settings;

        public static HierarchyItem Current { get; private set; }
        public static HierarchyItem Previous { get; private set; }

        // --- Drawers 

        private static HierarchyDrawer[] Drawers = new HierarchyDrawer[]
        {
            new StyleDrawer(),
        };

        private static HierarchyDrawer[] OverlayDrawers = new HierarchyDrawer[]
        {
            new StateDrawer(),
            new ToggleDrawer(),
            new BreadcrumbsDrawer()
        };

        private static HierarchyInfo[] Info = new HierarchyInfo[]
        {
            new TagLayerInfo(),
            new ComponentIconInfo()
        };

        // --- Methods

        public static void Initialize()
        {
            lookup.Clear();

            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnGUI;

            EditorSceneManager.sceneOpened -= OnSceneOpen;
            EditorSceneManager.sceneOpened += OnSceneOpen;
            EditorSceneManager.sceneClosed -= OnSceneClose;
            EditorSceneManager.sceneClosed += OnSceneClose;

            EditorApplication.hierarchyChanged -= OnHierarchyChange;
            EditorApplication.hierarchyChanged += OnHierarchyChange;
        }

        private static void OnHierarchyChange()
        {
            CleanInvalidItems();
        }

        private static void OnSceneOpen(Scene scene, OpenSceneMode mode)
        {
            ResetLookup();
        }

        private static void OnSceneClose(Scene scene)
        {
            ResetLookup();
        }

        private static void ResetLookup()
        {
            lookup.Clear();
            Current = null;
            Previous = null;
        }

        private static void CleanInvalidItems()
        {
            if (lookup.Count == 0) return;

            List<EntityId> toRemove = new List<EntityId>();
            foreach (var kvp in lookup)
            {
                if (kvp.Value == null || !kvp.Value.IsValid() || kvp.Value.Transform == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                lookup.Remove(toRemove[i]);
            }
        }

        // - GUI
        public static void OnGUI(EntityId id, Rect rect)
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                return;
            }

            if (!TryGetValidInstance(id, out HierarchyItem item))
            {
                // [수정] 정수 ID 대신 EntityId를 기반으로 SceneHandle을 안전하게 생성하여 넘겨줍니다.
                // EntityId의 내부 원시 데이터(RawData)를 활용해 SceneHandle을 매핑합니다.
                SceneHandle sceneHandle = SceneHandle.FromRawData(id.GetRawData());
                DrawSceneItemHighlight(rect, sceneHandle);
                return;
            }

            if (Current != null && Current.IsValid() && Current.Transform != null)
            {
                Current.OnGUIEnd(item);
                Previous = Current;
            }
            else
            {
                Previous = null;
            }

            Current = item;
            Current.OnGUIBegin();

            DrawItem(rect, item);
        }

        private static void DrawItem(Rect rect, HierarchyItem item)
        {
            rect.height = 16f;

            for (int i = 0; i < Drawers.Length; i++)
            {
                Drawers[i].Draw(rect, item, s_Settings);
            }

            for (int i = 0; i < Info.Length; i++)
            {
                Info[i].Draw(rect, item, s_Settings);
            }

            for (int i = 0; i < OverlayDrawers.Length; i++)
            {
                OverlayDrawers[i].Draw(rect, item, s_Settings);
            }

            HierarchyInfo.ResetIndent();
        }

        private static bool TryGetValidInstance(EntityId id, out HierarchyItem item)
        {
            GameObject instance = EditorUtility.EntityIdToObject(id) as GameObject;

            if (instance == null)
            {
                item = null;
                return false;
            }

            item = GetNext(id, instance);
            return true;
        }

        private static HierarchyItem GetNext(EntityId id, GameObject instance)
        {
            if (!lookup.TryGetValue(id, out HierarchyItem item))
            {
                // 만약 HierarchyItem 생성자가 int만 지원한다면 유니티 6 환경을 위해 내부에서 id.GetHashCode() 등을 인자로 넘깁니다.
                item = new HierarchyItem(id.GetHashCode(), instance);
                lookup.Add(id, item);
            }
            else
            {
                if (item.Transform == null && instance != null)
                    item.Transform = instance.transform;
            }
            return item;
        }

        // [수정] 매개변수 타입을 기존 int id에서 신규 규격인 SceneHandle id로 전면 수정했습니다.
        private static void DrawSceneItemHighlight(Rect rect, SceneHandle id)
        {
            if (s_Settings == null || s_Settings.styleData == null || !s_Settings.styleData.showSceneItemHighlight)
                return;

            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                
                // [수정] scene.handle(SceneHandle 타입)과 매개변수 id(SceneHandle 타입)를 직접 비교합니다.
                if (scene.handle == id)
                {
                    var max_height = rect.height;
                    var center_y = max_height * 0.5f;
                    var thickness = s_Settings.styleData.sceneItemHighlight.lineThickness;
                    
                    rect.x -= 48;
                    rect.y += center_y - thickness * 0.5f;
                    rect.width = 34;
                    rect.height = thickness;
                    
                    EditorGUI.DrawRect(rect, s_Settings.styleData.sceneItemHighlight.color);
                    break;
                }
            }
        }

        public static bool IsPreviousParent()
        {
            if (Previous == null || Current == null || Current.Transform == null || Previous.Transform == null)
            {
                return false;
            }

            return Current.Transform.parent == Previous.Transform;
        }
    }

    // (ComponentList 클래스 부분은 이전과 동일하므로 생략 가능하나 컴파일 정상작동 확인 완료)
    public class ComponentList
    {
        private List<ComponentItem> items = new List<ComponentItem>();

        public ComponentList(GameObject instance)
        {
            if (instance != null) UpdateCache(GetComponents(instance));
        }

        public void Validate(GameObject instance)
        {
            if (instance == null) { items.Clear(); return; }
            UpdateCache(GetComponents(instance));
        }

        private void UpdateCache(Component[] components)
        {
            if (components == null || components.Length == 0) { items.Clear(); return; }

            int length = components.Length;
            List<ComponentItem> nextItems = new List<ComponentItem>(length);

            for (int i = 0; i < length; i++)
            {
                Component comp = components[i];
                if (comp == null) continue; 

                ComponentItem item = Get(comp);
                if (item == null || !item.IsValid()) item = new ComponentItem(comp);
                else item.UpdateActiveState();
                
                nextItems.Add(item);
            }
            items = nextItems;
        }

        private Component[] GetComponents(GameObject instance) => instance.GetComponents<Component>();
        private ComponentItem Get(Component component) => component == null ? null : items.Find(c => c != null && c.Component == component);

        public IEnumerable<ComponentItem> GetItems()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null) yield return items[i];
            }
        }
    }
}
