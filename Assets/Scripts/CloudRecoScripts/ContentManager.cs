/*============================================================================== 
 * Copyright (c) 2012-2014 Qualcomm Connected Experiences, Inc. All Rights Reserved. 
 * ==============================================================================*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vuforia;

/// <summary>
/// This class manages the content displayed on top of cloud reco targets in this sample
/// </summary>
public class ContentManager : MonoBehaviour, ITrackableEventHandler
{
    #region EXPOSED_PUBLIC_VARIABLES

    /// <summary>
    /// The spinning wheel rendered while data is fetched from the server
    /// </summary>
    public Texture LoadingSpinner;

    /// <summary>
    /// The background texture behind the spinning wheel
    /// </summary>
    public Texture LoadingBackground;
    
    /// <summary>
    /// The root gameobject that serves as an augmentation for the image targets created by search results
    /// </summary>
    public GameObject AugmentationObject;
    
    /// <summary>
    /// Reference to the script handling animations between 2D and 3D.
    /// </summary>
    public AnimationsManager AnimationsManager;

    /// <summary>
    /// the URL the JSON data should be fetched from
    /// </summary>
    public string JsonServerUrl;

    #endregion  // EXPOSED_PUBLIC_VARIABLES



    #region PRIVATE_MEMBER_VARIABLES

    private bool mIsShowingBookData = false;
    private bool mIsLoadingBookData = false;
    private bool mIsLoadingBookThumb = false;
    
    private WWW mJsonBookInfo;
    private WWW mBookThumb;
    
	private string mJsonBookInfo_new;

    private BookData mBookData;
    private bool mIsBookThumbRequested = false;
    private BookInformationParser mBookInformationParser;
    private bool mIsShowingMenu = false;
    private CloudRecoBehaviour mCloudRecoBehaviour;
    private Dictionary<string, GUIStyle> mButtonGUIStyles;
    private Touch mTouch;
    private GUISkin mUISkin;
    private RaycastHit mHit;

     /// <summary>
    /// This float returns a resolution dependent scale factor.
    /// Using this, elements can be drawn as if the resolution was 480 (smaller dimension)
    /// on every device.
    /// </summary>
    private static float DeviceDependentScale
    {
        get 
        {
            if ( Screen.width > Screen.height)
                return Screen.height / 480f;
            else 
                return Screen.width / 480f; 
        }
    }
    
    #endregion // PRIVATE_MEMBER_VARIABLES
    
    void Start ()
    {
        // setup BookInformationParser 
        mBookInformationParser = new BookInformationParser();
        mBookInformationParser.SetBookObject(AugmentationObject);

        TrackableBehaviour trackableBehaviour = AugmentationObject.transform.parent.GetComponent<TrackableBehaviour>();
        if (trackableBehaviour)
        {
            trackableBehaviour.RegisterTrackableEventHandler(this);
        }
        
         mCloudRecoBehaviour = (CloudRecoBehaviour)FindObjectOfType(typeof(CloudRecoBehaviour));
        
         mUISkin = Resources.Load("UserInterface/ButtonSkins") as GUISkin;
        
        mButtonGUIStyles = new Dictionary<string,GUIStyle>();
        foreach (GUIStyle customStyle in mUISkin.customStyles) 
            mButtonGUIStyles.Add(customStyle.name, customStyle);
        
        HideObject();
    }
    
    void Update () 
    {
        if( mIsShowingBookData )
        {
            if(Input.GetMouseButtonUp(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast (ray, out mHit, 1000.0f)) {
                    GameObject hitObject = mHit.collider.gameObject;
                    if(hitObject != null && hitObject.name == "BookInformation" )                        
                    {
                        if (mBookData != null && mIsShowingMenu == false)
                        {
                            Application.OpenURL(mBookData.BookDetailUrl);
                        }
                    }
                }
            }
        }

        if(mIsLoadingBookThumb)
        {
            LoadBookThumb();
        }
    }

    void OnGUI()
    {
        if(mIsLoadingBookData || mIsLoadingBookThumb)
        {
            // Draws the rotating loading spinner
            float thisAngle = Time.frameCount * 4;
            Rect spinnerRect = new Rect(Screen.width/2.0f -LoadingSpinner.width/2f, Screen.height/2.0f - LoadingSpinner.height/2f, LoadingSpinner.width, LoadingSpinner.height);
            DrawTexture(spinnerRect, LoadingSpinner, true, thisAngle, spinnerRect);
        }
        if (mCloudRecoBehaviour.CloudRecoInitialized && !mCloudRecoBehaviour.CloudRecoEnabled)
        {
            GUIStyle cancelButtonStyle;
            if (mButtonGUIStyles.TryGetValue("CancelButton", out cancelButtonStyle))
            {
                float buttonWidth = cancelButtonStyle.normal.background.width * DeviceDependentScale;
                float buttonHeight = cancelButtonStyle.normal.background.height * DeviceDependentScale;
                
                if (Button(new Rect(Screen.width - buttonWidth, 0, buttonWidth, buttonHeight),string.Empty,cancelButtonStyle))
                {
                    mCloudRecoBehaviour.CloudRecoEnabled = true;
                    TargetDeleted();
                }
            }
        }
    }

    void DrawTexture(Rect rect, Texture texture, bool rotateAroundPivot, float angle, Rect pivotRect)
    {
        // Draws the Loading background
        GUI.DrawTexture (pivotRect, LoadingBackground);
        Matrix4x4 oldMatrix = GUI.matrix;
        if(rotateAroundPivot)
        {
            GUIUtility.RotateAroundPivot (angle, pivotRect.center);
        }
        GUI.DrawTexture (rect, texture);
        GUI.matrix = oldMatrix;
    }

    bool Button(Rect rect, string text, GUIStyle style)
    {
        return GUI.Button(rect, text, style);
    }

    /// <summary>
    /// Method called from the CloudRecoEventHandler
    /// when a new target is created
    /// </summary>
    public void TargetCreated(string targetMetadata)
    {
        // Initialize the showing book data variable
        mIsShowingBookData = true;
        mIsLoadingBookData = true;
        
        mIsBookThumbRequested = false;
            
        // Loads the JSON Book Data
        StartCoroutine( LoadJSONBookData(targetMetadata) );
    }
    

    /// <summary>
    /// Method called when the Close button is pressed
    /// to clean the target Data
    /// </summary>
    public void TargetDeleted()
    {
        // Initialize the showing book data variable
        mIsShowingBookData = false;
        mIsLoadingBookData = false;
        mIsLoadingBookThumb = false;
        
        mBookData = null;
    }

    /// <summary>
    /// Implementation of the ITrackableEventHandler function called when the
    /// tracking state changes.
    /// </summary>
    public void OnTrackableStateChanged(
                                    TrackableBehaviour.Status previousStatus,
                                    TrackableBehaviour.Status newStatus)
    {
        if (newStatus == TrackableBehaviour.Status.DETECTED ||
            newStatus == TrackableBehaviour.Status.TRACKED)
        {
            TargetFound();
        }
        else
        {
            TargetLost();
        }
    }

    /// <summary>
    /// hides the augmentation object
    /// </summary>
    public void HideObject()
    {
        Renderer[] rendererComponents = AugmentationObject.GetComponentsInChildren<Renderer>();
        Collider[] colliderComponents = AugmentationObject.GetComponentsInChildren<Collider>();

        // Enable rendering:
        foreach (Renderer component in rendererComponents)
        {
            component.enabled = false;
        }

        // Enable colliders:
        foreach (Collider component in colliderComponents)
        {
            component.enabled = false;
        }
    }


    /// <summary>
    /// Method to let the ContentManager know if the CloudReco
    /// SampleMenu is being displayed
    /// </summary>
    public void SetIsShowingMenu(bool isShowing)
    {
        mIsShowingMenu = isShowing;
    }

    /// <summary>
    /// Method called from the CloudReco Trackable Event Handler
    /// when a target has been found
    /// </summary>
    private void TargetFound()
    {
        // Checks tha the book info is displayed
        if (mIsShowingBookData)
        {
            // Starts playing the animation to 3D
            AnimationsManager.PlayAnimationTo3D(AugmentationObject);
        }
    }


    /// <summary>
    /// Method called from the CloudReco Trackable Event Handler
    /// when a target has been Lost
    /// </summary>
    private void TargetLost()
    {
        // Checks tha the book info is displayed
        if (mIsShowingBookData)
        {
            // Starts playing the animation to 2D
            AnimationsManager.PlayAnimationTo2D(AugmentationObject);
        }
    }

    /// <summary>
    /// fetches the JSON data from a server
    /// </summary>
    private IEnumerator LoadJSONBookData(string jsonBookUrl)
    {

        // Gets the full book json url
        string fullBookURL = JsonServerUrl + jsonBookUrl;
    
        // Gets the json book info from the url
        mJsonBookInfo = new WWW(fullBookURL);
        yield return mJsonBookInfo;

        // Loading done
        mIsLoadingBookData = false;
        
        if (mJsonBookInfo.error == null)
        {
            // Parses the json Object
            JSONParser parser = new JSONParser();
                
            BookData bookData = parser.ParseString(mJsonBookInfo.text);
            mBookData = bookData;
        
            // Updates the BookData info in the augmented object
            mBookInformationParser.UpdateBookData(bookData);
        
            mIsLoadingBookThumb = true;
        } 
        else
        {
			//Debug.LogError("Error downloading json");
			BookData bookData = new BookData();
			bookData.SetBookTitle("The Hunger Game");
			bookData.SetBookAuthor("Suzanne Collins");
			bookData.SetBookRating(4);
			bookData.SetBookOverallRating(23933);
			bookData.SetBookRegularPrice(10.99f);
			bookData.SetBookYourPrice(7.09f);
			bookData.SetBookThumbUrl("http://ecx.images-amazon.com/images/I/41bOj-am1RL._SX331_BO1,204,203,200_.jpg");
			bookData.SetBookDetailUrl("http://www.amazon.com/The-Hunger-Games-Book-1/dp/0439023521/ref=pd_bxgy_14_img_z");

			mBookData = bookData;
			Debug.Log("mBookData assigned");

			
			// Updates the BookData info in the augmented object
			mBookInformationParser.UpdateBookData(bookData);
			
			mIsLoadingBookThumb = true;
		}
    }
    
    /// <summary>
    /// Loads the texture for the book thumbnail
    /// </summary>
    private void LoadBookThumb()
    {
        if(!mIsBookThumbRequested )            
        {
            if(mBookData != null )
            {
                mBookThumb = new WWW(mBookData.BookThumbUrl);
            
                mIsBookThumbRequested = true;
            }
        }
        
        if(mBookThumb.progress >=1)
        {
            if(mBookThumb.error == null && mBookData != null)
            {
                mBookInformationParser.UpdateBookThumb(mBookThumb.texture);
                mIsLoadingBookThumb = false;

                ShowObject();
            }
        }
    }
    
    /// <summary>
    /// shows the augmentation object
    /// </summary>
    private void ShowObject()
    {
        Renderer[] rendererComponents = AugmentationObject.GetComponentsInChildren<Renderer>();
        Collider[] colliderComponents = AugmentationObject.GetComponentsInChildren<Collider>();

        // Enable rendering:
        foreach (Renderer component in rendererComponents)
        {
            component.enabled = true;
        }

        // Enable colliders:
        foreach (Collider component in colliderComponents)
        {
            component.enabled = true;
        }
    }

}
