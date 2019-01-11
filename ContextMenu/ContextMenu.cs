using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using SimpleJSON;

namespace DeluxePlugin
{
    public class ContextMenu: MVRScript
    {
        protected Dictionary<string, float> morphDict = new Dictionary<string, float>();
        protected JSONStorableFloat defautHeight;

        List<Atom> buttons = new List<Atom>();
        int buttonIndex = 0;
        const float BUTTON_SPACING = .07f;

        public override void Init()
        {
            try
            {
              defautHeight = new JSONStorableFloat("Default height", heightByDef, 0.0f, 2.0f, true);
              defautHeight.storeType = JSONStorableParam.StoreType.Full;
              RegisterFloat(defautHeight);
              CreateSlider(defautHeight, true);

              StartCoroutine(Setup());
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        private void saveLoadMorphs(bool saveM)
        {
            JSONStorable geometry = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;

            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
              if(saveM){
                morphDict[name] = morphControl.GetMorphByDisplayName(name).morphValue;
              }else{
                morphControl.GetMorphByDisplayName(name).morphValue = morphDict[name];
              }
            });
        }


        IEnumerator Setup()
        {
            yield return new WaitForSeconds(1.0f);
            CreateWorldButton("Select Main", () =>
            {
                SuperController.singleton.ShowMainHUD();
                SuperController.singleton.SelectController(containingAtom.mainController);
            });

            CreateWorldButton("Load Appearance", () =>
            {
                SuperController.singleton.ShowMainHUD();
                SuperController.singleton.SelectController(containingAtom.mainController);
                containingAtom.LoadAppearancePresetDialog();
            });

            CreateWorldButton("Load Pose", () =>
            {
                SuperController.singleton.ShowMainHUD();
                SuperController.singleton.SelectController(containingAtom.mainController);
                containingAtom.LoadPhysicalPresetDialog();
            });

            CreateWorldButton("Quicksave Morphs", () =>
            {
                saveLoadMorphs(true);
            });

            CreateWorldButton("Quickload Morphs", () =>
            {
                saveLoadMorphs(false);
            });
        }

        void CreateWorldButton(string buttonLabel, UnityEngine.Events.UnityAction call)
        {
            StartCoroutine(CreateWorldButtonCO(buttonLabel, call, buttonIndex++));
        }

        IEnumerator CreateWorldButtonCO(string buttonLabel, UnityEngine.Events.UnityAction call, int index)
        {
            Atom containingAtom = GetContainingAtom();

            string name = containingAtom.uid;
            string uid = name + " " + buttonLabel;

            Atom atom = SuperController.singleton.GetAtomByUid(uid);
            if (atom == null)
            {
                yield return SuperController.singleton.AddAtomByType("UIButton", uid);
            }
            else
            {
                Debug.Log("button already exists");
                if (buttons == null)
                {
                    buttons = new List<Atom>();
                }
                buttons.Add(atom);
                yield return null;
            }

            atom = SuperController.singleton.GetAtomByUid(uid);

            atom.GetStorableByID("Text").SetStringParamValue("text", buttonLabel);

            UIButtonTrigger ubt = atom.GetStorableByID("Trigger") as UIButtonTrigger;
            ubt.button.onClick.AddListener(call);

            atom.GetStorableByID("Canvas").SetFloatParamValue("xSize", 500);
            atom.GetStorableByID("Canvas").SetFloatParamValue("ySize", 100);
            HSVColor black = new HSVColor();
            black.H = 0; black.S = 0; black.V = .12f;

            HSVColor gray = new HSVColor();
            gray.H = 0; gray.S = 0; gray.V = 0.75f;

            atom.GetStorableByID("ButtonColor").SetColorParamValue("color", black);
            atom.GetStorableByID("TextColor").SetColorParamValue("color", gray);

            atom.GetStorableByID("Text").SetFloatParamValue("fontSize", 50);

            FreeControllerV3 controller = atom.GetStorableByID("control") as FreeControllerV3;
            controller.deselectedMeshScale = 0.000f;

            SphereCollider collider = controller.GetComponent<SphereCollider>();
            collider.radius = 0.0f;
            collider.enabled = false;

            atom.SetOn(false);

            buttons.Add(atom);
        }

        void RestorePosition(Atom atom, FreeControllerV3 controller, int index)
        {
            if (atom == null)
            {
                return;
            }

            if (controller == null)
            {
                return;
            }

            Transform tc = SuperController.singleton.lookCamera.transform;
            Quaternion cr = tc.rotation;
            Vector3 pc  = tc.position;

            float currentHeight = (pc[1]-defautHeight.val)/3+defautHeight.val;
            cr[0] = 0;
            cr[2] = 0;

            atom.mainController.transform.SetPositionAndRotation(controller.transform.position, cr);
            atom.mainController.transform.Translate(defaultDistance, currentHeight, 0, Space.Self);
            atom.mainController.transform.Translate(0, -BUTTON_SPACING * index, 0, Space.World);
            atom.mainController.transform.LookAt(pc);

        }

        bool lastVisibility = true;
        FreeControllerV3 lastController;
        void Update()
        {
            try
            {
              bool visibilityState = SuperController.singleton.GetSelectedAtom() == containingAtom;

                bool visChanged = false;
                if (visibilityState != lastVisibility)
                {
                    visChanged = true;
                    lastVisibility = visibilityState;
                }

                bool controlChanged = lastController != SuperController.singleton.GetSelectedController();
                if (controlChanged)
                {
                    lastController = SuperController.singleton.GetSelectedController();
                }

                int index = 0;
                buttons.ForEach((button) =>
                {
                    button.containingAtom.SetOn(visibilityState);

                    if (visChanged || lastController)
                    {
                        RestorePosition(button, containingAtom.mainController, index);
                    }
                    index++;
                });

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }

        }

        void OnDestroy()
        {
            buttons.ForEach((button) =>
            {
                if (button != null)
                {
                    SuperController.singleton.RemoveAtom(button);
                }
            });
        }

        private float defaultDistance = -0.45f;
        private float heightByDef = 1.6f;
    }
}
