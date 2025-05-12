using UnityEditor;
using UnityEngine;
using PlayTable;

#if UNITY_EDITOR
namespace PlayTable
{
    public class PTMenu : MonoBehaviour
    {
        public static PTMenu singleton = null;

        private static readonly Vector3 CAMERA_POSITION = new Vector3(0, 35, 0);
        private static readonly Vector3 CAMERA_EULARANGLES = new Vector3(90, 0, 0);

        private void Reset()
        {
            if (singleton == null)
            {
                singleton = this;
            }
            else
            {
                Destroy(this);
            }
        }
        private void OnDestroy()
        {
            if (singleton == this)
            {
                singleton = null;
            }
        }
        // Add a menu item named "Do Something" to MyMenu in the menu bar.
        [MenuItem("PlayTable/Do Something")]
        static void DoSomething()
        {
            Debug.Log("Doing Something...");
        }

        // Validated menu item.
        // Add a menu item named "Log Selected Transform Name" to MyMenu in the menu bar.
        // We use a second function to validate the menu item
        // so it will only be enabled if we have a transform selected.
        [MenuItem("PlayTable/Log Selected Transform Name")]
        static void LogSelectedTransformName()
        {
            Debug.Log("Selected Transform is on " + Selection.activeTransform.gameObject.name + ".");
        }

        // Validate the menu item defined by the function above.
        // The menu item will be disabled if this function returns false.
        [MenuItem("PlayTable/Log Selected Transform Name", true)]
        static bool ValidateLogSelectedTransformName()
        {
            // Return false if no transform is selected.
            return Selection.activeTransform != null;
        }

        // Add a menu item named "Do Something with a Shortcut Key" to MyMenu in the menu bar
        // and give it a shortcut (ctrl-g on Windows, cmd-g on macOS).
        [MenuItem("PlayTable/Do Something with a Shortcut Key %g")]
        static void DoSomethingWithAShortcutKey()
        {
            Debug.Log("Doing something with a Shortcut Key...");
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("PlayTable/Set up tabletop")]
        static void SetupTabletop()
        {
            foreach (GameObject obj in FindObjectsOfType<GameObject>())
            {
                DestroyImmediate(obj);
            }
            CreateTabletopManager();
        }

        // Add a menu item called "Double Mass" to a Rigidbody's context menu.
        [MenuItem("CONTEXT/Rigidbody/Double Mass")]
        static void DoubleMass(MenuCommand command)
        {
            Rigidbody body = (Rigidbody)command.context;
            body.mass = body.mass * 2;
            Debug.Log("Doubled Rigidbody's Mass to " + body.mass + " from Context Menu.");
        }

        //Switch camera to perspective
        [MenuItem("CONTEXT/Camera/Perspective")]
        static void SwitchCameraToPerspective(MenuCommand command)
        {
            Camera camera = (Camera)command.context;
            camera.orthographic = false;
            camera.fieldOfView = 26;
            camera.transform.position = CAMERA_POSITION;
            camera.transform.eulerAngles = CAMERA_EULARANGLES;
        }

        //Switch camera to orthographic
        [MenuItem("CONTEXT/Camera/Orthographic")]
        static void SwitchCameraToOrthographic(MenuCommand command)
        {
            Camera camera = (Camera)command.context;
            camera.orthographic = true;
            camera.orthographicSize = 8.1f;
            camera.transform.position = CAMERA_POSITION;
            camera.transform.eulerAngles = CAMERA_EULARANGLES;
        }


        // Add a menu item to create custom GameObjects.
        // Priority 1 ensures it is grouped with the other menu items of the same kind
        // and propagated to the hierarchy dropdown and hierarchy context menus.
        [MenuItem("GameObject/PlayTable/PTObject", false, 10)]
        static void CreatePTObject(MenuCommand menuCommand)
        {
            // Create a custom game object
            PTTransform obj = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<PTTransform>();
            obj.name = "object";
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(obj.gameObject, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);
            Selection.activeObject = obj;
        }

        [MenuItem("GameObject/PlayTable/TabletopManager", false, 10)]
        static void CreateTabletopManager(MenuCommand menuCommand)
        {
            CreateTabletopManager();
        }
        static void CreateTabletopManager()
        {
            GameObject prefTabletopManager = Resources.Load<GameObject>(PTFramework.DIR_TTMANAGER);
            GameObject newObj = Instantiate(prefTabletopManager, Vector3.zero, Quaternion.identity);

            // Create a custom game object
            newObj.name = "TabletopManager";
            Undo.RegisterCreatedObjectUndo(newObj, "Create " + newObj.name);
            Selection.activeObject = newObj;
        }

        [MenuItem("GameObject/PlayTable/Table")]
        static void CreateTable(MenuCommand menuCommand)
        {
            // Create a custom game object
            PTTransform obj = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<PTTransform>();
            obj.name = "object";
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(obj.gameObject, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);
            Selection.activeObject = obj;
        }
    }
}
#endif
