/*============================================================================== 
 * Copyright (c) 2012-2014 Qualcomm Connected Experiences, Inc. All Rights Reserved. 
 * ==============================================================================*/

using UnityEngine;
using System.Collections;
using Vuforia;

public class CloudRecoSceneManager : MonoBehaviour
{

    #region PRIVATE_MEMBER_VARIABLES
    
    private SampleInitErrorHandler mPopUpMsg;
    private bool mErrorOccurred;
    private CameraDevice.FocusMode mFocusMode;
    
    #endregion PRIVATE_MEMBER_VARIABLES

    #region UNITY_MONOBEHAVIOUR

    void Awake()
    {
        mPopUpMsg = GetComponent<SampleInitErrorHandler>();
        if (!mPopUpMsg)
        {
            mPopUpMsg = gameObject.AddComponent<SampleInitErrorHandler>();
        }

        // Check for an initialization error on start.
        VuforiaAbstractBehaviour vuforiaBehaviour = (VuforiaAbstractBehaviour)FindObjectOfType(typeof(VuforiaAbstractBehaviour));
        if (vuforiaBehaviour)
        {
            vuforiaBehaviour.RegisterVuforiaInitErrorCallback(OnVuforiaInitializationError);
        }
    }
    void Start () 
    {
        InputController.BackButtonTapped += OnBackButtonTapped;
        InputController.SingleTapped += OnSingleTapped;

        //Enable continuous autofocus on start; register Vuforia started callback
        VuforiaAbstractBehaviour vuforiaBehaviour = (VuforiaAbstractBehaviour)FindObjectOfType(typeof(VuforiaAbstractBehaviour));
        if (vuforiaBehaviour)
        {
            vuforiaBehaviour.RegisterVuforiaStartedCallback(SetFocusModeToContinuousAuto);
        }

        if(mErrorOccurred)
        {
            mPopUpMsg.InitPopUp();
        }
    }
    
    void Update () 
    {
        if (mErrorOccurred)
            return;

        InputController.UpdateInput();
    }

    void OnGUI()
    {
        if (mErrorOccurred)
        {
            mPopUpMsg.Draw();
            return;
        }
    }
    
    void OnDestroy()
    {
        // unregister callback methods
        VuforiaAbstractBehaviour vuforiaBehaviour = (VuforiaAbstractBehaviour)FindObjectOfType(typeof(VuforiaAbstractBehaviour));
        if (vuforiaBehaviour)
        {
            vuforiaBehaviour.UnregisterVuforiaStartedCallback(SetFocusModeToContinuousAuto);
            vuforiaBehaviour.UnregisterVuforiaInitErrorCallback(OnVuforiaInitializationError);
        }
    }

    #endregion //UNITY_MONOBEHAVIOUR

    #region PRIVATE_METHODS

    private void OnBackButtonTapped()
    {
        Application.LoadLevel("Vuforia-1-About");
    }
    
    //Setting focus mode to triggerauto unsets the continuous autofocus mode. So, we invoke continuous autofocus right after.
    private void OnSingleTapped()
    {
        StartCoroutine(SetFocusModeToTriggerAuto());
    }
    
    private IEnumerator SetFocusModeToTriggerAuto()
    {
        if (CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_TRIGGERAUTO)) {
              mFocusMode = CameraDevice.FocusMode.FOCUS_MODE_TRIGGERAUTO;
        }
        
        Debug.Log("Focus Mode Changed To " + mFocusMode);
        
        yield return new WaitForSeconds(1.0f);
        
        SetFocusModeToContinuousAuto();
        
    }
    
    private void SetFocusModeToContinuousAuto()
    {
        if (CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO)) {
            mFocusMode = CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO;
        }
        
        Debug.Log("Focus Mode Changed To " + mFocusMode);
    }

    public void OnVuforiaInitializationError(VuforiaUnity.InitError initError)
    {
        if (initError != VuforiaUnity.InitError.INIT_SUCCESS)
        {
            mErrorOccurred = true;
            mPopUpMsg.SetErrorCode(initError);
        }
    }

    #endregion //PRIVATE_MEMBERS

}
