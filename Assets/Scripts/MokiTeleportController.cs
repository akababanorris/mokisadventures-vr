/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MokiTeleportController : MonoBehaviour {

    public float maxTeleportRange;
    public OVRInput.Button teleportButton;
    public KeyCode teleportKey;
    public Transform pointerTransform; // Could be a tracked controller
    public bool allowForRealHeadRotation;
    public float realHeadMovementCompensation;
    public float rotationSpeed = 1;

    public float fadeSpeed = 0.5f;
    public float fadeLength = 2f;

    public float rotateStickThreshold = 0.5f;

	public GameObject endInfoCard;

    [HideInInspector()]
    public bool teleportEnabled = true;

    public LayerMask teleportLayerMask;

    private MokiTeleportPoint currentTeleportPoint;
    private float rotationAmount;
    private Quaternion initialRotation;
    private bool teleporting = false;

	private OVRCameraRig cameraRig;
	private OVRScreenFade2 fader;

	private AsyncOperation async;

	private bool movedCameraOnLoad = false;

	void Awake()
	{
		cameraRig = GameObject.Find("OVRCameraRig").GetComponent<OVRCameraRig>();
		cameraRig.EnsureGameObjectIntegrity();
		// make sure we have a new fader object
		fader = cameraRig.GetComponentInChildren<OVRScreenFade2>();
		if (fader == null)
		{
			GameObject fadeObj = Instantiate(Resources.Load("Prefabs/Fader", typeof(GameObject))) as GameObject;
			Debug.Log ("fadeObj " + fadeObj);
			fadeObj.transform.SetParent(cameraRig.centerEyeAnchor, false);
			fader = fadeObj.GetComponent<OVRScreenFade2>();
		}
		fader.PositionForCamera(cameraRig);

		// Make sure legacy fader objects are not present
		if (cameraRig.leftEyeAnchor.GetComponent<OVRScreenFade>() != null ||
			cameraRig.rightEyeAnchor.GetComponent<OVRScreenFade>() != null)
		{
			Debug.LogError("Camera rig has ScreenFade objects");
		}
	}
	//3:38 - 4:07
	void OnLevelWasLoaded (int index)
	{
		if (!movedCameraOnLoad) {
			movedCameraOnLoad = true;
			if (endInfoCard) {
				endInfoCard.SetActive (true);
			}
			MokiTeleportPoint teleport01Transform = GameObject.Find("TeleportPoint-01").GetComponent<MokiTeleportPoint>();
			Transform transform01 = teleport01Transform.GetDestTransform ();
			transform.position = transform01.position;
			transform.rotation = transform01.rotation;
		}
	}

	// Update is called once per frame
	void Update () {
        RaycastHit hit;
		if (currentTeleportPoint && teleporting)
        {
            if (OVRInput.GetUp(teleportButton) || Input.GetKeyUp(KeyCode.Space) || Input.GetMouseButtonUp(0))
            {
				if (currentTeleportPoint.name == "TeleportPoint-00") 
				{
					DoSwitchScene ("moki_360_turtle_scene");
				} else {
					DoTeleport(currentTeleportPoint.GetDestTransform());
				}
            }
        }
        else if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out hit, maxTeleportRange, teleportLayerMask))
        {
            MokiTeleportPoint tp = hit.collider.gameObject.GetComponent<MokiTeleportPoint>();
            tp.OnLookAt();

            if (teleportEnabled && !teleporting && (OVRInput.GetDown(teleportButton) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                StartTeleport(tp);
            }
            
        }
	}

    void StartTeleport(MokiTeleportPoint tp)
    {
        teleporting = true;
        
		if (currentTeleportPoint) {
			currentTeleportPoint.GetComponent<MeshRenderer> ().enabled = true;
			if (currentTeleportPoint.infoCard) {
				currentTeleportPoint.infoCard.SetActive (false);
			}
		} else {
			//At first position, find first card to disable
			GameObject card1 = GameObject.Find ("InfoCard-06");
			if (card1) {
				card1.SetActive (false);
			}
		}
        currentTeleportPoint = tp;
        currentTeleportPoint.GetComponent<MeshRenderer>().enabled = false;

        rotationAmount = 0;
    }

    void DoTeleport(Transform destTransform)
    {
        StartCoroutine(TeleportCoroutine(destTransform));
    }

	void DoSwitchScene(string sceneName)
	{
		StartCoroutine(SwitchSceneCoroutine(sceneName));
	}

	IEnumerator SwitchSceneCoroutine(string sceneName)
	{
		float fadeLevel = 0;
		async = Application.LoadLevelAsync(sceneName);
		async.allowSceneActivation = false;
		while (fadeLevel < 1)
		{
			yield return null;
			fadeLevel += fadeSpeed * Time.deltaTime;
			fadeLevel = Mathf.Clamp01(fadeLevel);
			fader.SetFadeLevel(fadeLevel);
		}
			
		async.allowSceneActivation = true;
		while (!async.isDone) {
			yield return null;
		}

		yield return null;
	}

    IEnumerator TeleportCoroutine(Transform destTransform)
    {
        Vector3 destPosition = destTransform.position;
        Quaternion destRotation = destTransform.rotation;

        float fadeLevel = 0;

        while (fadeLevel < 1)
        {
            yield return null;
            fadeLevel += fadeSpeed * Time.deltaTime;
            fadeLevel = Mathf.Clamp01(fadeLevel);
			fader.SetFadeLevel(fadeLevel);
        }
			
        transform.position = destPosition;
       
        if (allowForRealHeadRotation)
        {
            Quaternion headRotation = UnityEngine.VR.InputTracking.GetLocalRotation(UnityEngine.VR.VRNode.Head);
            Vector3 euler = headRotation.eulerAngles;
            euler.x = 0;
            euler.z = 0;
            headRotation = Quaternion.Euler(euler);
            transform.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(headRotation), realHeadMovementCompensation) * destRotation;
        }
        else
        {
            transform.rotation = destRotation;
        }

		if (currentTeleportPoint.infoCard) {
			currentTeleportPoint.infoCard.SetActive (true);
		};

        yield return new WaitForSeconds(fadeLength);

        teleporting = false;

        while (fadeLevel > 0)
        {
            yield return null;
            fadeLevel -= fadeSpeed * Time.deltaTime;
            fadeLevel = Mathf.Clamp01(fadeLevel);
			fader.SetFadeLevel(fadeLevel);
        }
			
        yield return null;
    }
}
