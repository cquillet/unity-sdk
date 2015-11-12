﻿using IBM.Watson.Utilities;
using UnityEngine;
using System.Collections;
using IBM.Watson.Logging;

using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace IBM.Watson.Widgets.Question
{
    public class PassageAnimationManager : WatsonBaseAnimationManager
    {

        #region Private Members

        private CubeAnimationManager m_CubeAnimMgr = null;
        private QuestionWidget m_QuestionWidget = null;
        private Transform[] m_PassageItems = null;
        private bool m_IsTouchOnDragging = false;   //Used to identify the release finger

        //Holds all animations current Ratio value
        float[] m_AnimationLocationRatio;
        float[] m_AnimationRotationRatio;

        //Passage One Finger Animation for first item path in Vector3
        Vector3[] m_PathToCenterForFirstItem;
        Vector3[] m_PathOrientationToCenterForFirstItem;
        Vector3[] m_PathToStackForFirstItem;
        Vector3[] m_PathOrientationToStackForFirstItem;

        //Passage one finger animation paths for each passage
        LTBezierPath[] m_BezierPathToCenter;
        LTBezierPath[] m_BezierPathOrientationToCenter;
        LTBezierPath[] m_BezierPathToStack;
        LTBezierPath[] m_BezierPathOrientationToStack;
        //For speedy movements we are using from Initial to Stack animation
        LTBezierPath[] m_BezierPathFromInitialToStack;
        LTBezierPath[] m_BezierPathOrientationFromInitialToStack;

        //Offset between passages
        Vector3 m_OffsetPathToCenter;
        Vector3 m_OffsetPathOrientationToCenter;
        Vector3 m_OffsetPathToStack;
        Vector3 m_OffsetPathOrientationToStack;

        //Target Locations / Orientations
        Vector3[] m_TargetLocation;
        Vector3[] m_TargetRotation;

        #endregion

        #region Public Members
        /// <summary>
        /// Gets the Cube Animation Manager attached with question widget. 
        /// </summary>
        /// <value>The cube.</value>
        public CubeAnimationManager Cube
        {
            get
            {
                if (m_CubeAnimMgr == null)
                    m_CubeAnimMgr = GetComponentInParent<CubeAnimationManager>();

                if (m_CubeAnimMgr == null)
                    Log.Error("PassageAnimationManager", "CubeAnimationManager is not found on parent of passage animation manager");

                return m_CubeAnimMgr;
            }
        }

        /// <summary>
        /// Gets the Question Widget attached 
        /// </summary>
        public QuestionWidget Question
        {
            get
            {
                if (m_QuestionWidget == null)
                    m_QuestionWidget = GetComponentInParent<QuestionWidget>();

                if (m_QuestionWidget == null)
                    Log.Error("PassageAnimationManager", "QuestionWidget is not found on parent of passage animation manager");

                return m_QuestionWidget;
            }
        }

        public Transform[] PassageList
        {
            get
            {
                if (m_PassageItems == null)
                {
                    //UpdatePassages();
                    if (m_PassageItems == null)
                    {
                        Log.Error("PassageAnimationManager", "PassageList couldn't find inside gameobject");
                    }
                }

                return m_PassageItems;
            }
        }

        public int NumberOfPassages
        {
            get
            {
                int numberOfPassage = 0;
                if (PassageList != null && PassageList.Length > 0)
                    numberOfPassage = PassageList.Length;
                return numberOfPassage;
            }
        }
        #endregion

        #region Awake / Update

        // Use this for initialization
        void Awake()
        {
            m_PathToCenterForFirstItem = new Vector3[]{
            new Vector3(-555,   -77,    -125),
            new Vector3(-600,   0,      -125),
            new Vector3(-800,   300,    -125),
            new Vector3(-907,   355,    -125)};

            m_PathOrientationToCenterForFirstItem = new Vector3[]{
            new Vector3(0,0,0),
            new Vector3(10,10,0),
            new Vector3(35,35,0),
            new Vector3(45,45,0)};

            m_PathToStackForFirstItem = new Vector3[]{
                new Vector3(-907,   355f,       -125),
                new Vector3(-800,   300f,       -200),
                new Vector3(450,    -100f,      -350),
                new Vector3(575,    -120,       -407)};

            m_PathOrientationToStackForFirstItem = new Vector3[]{
                new Vector3(45,45,0),
                new Vector3(35,35,0),
                new Vector3(10,10,0),
                new Vector3(0,0,0)};

            m_OffsetPathToCenter = new Vector3(0, 0, 50);
            m_OffsetPathOrientationToCenter = new Vector3(0, 0, 0);
            m_OffsetPathToStack = new Vector3(0, 0, 50);
            m_OffsetPathOrientationToStack = new Vector3(0, 0, 0);


        }

        void Start()
        {
            UpdatePassages();
        }

        // Update is called once per frame
        void Update()
        {
            DragOneFingerOnPassageOnUpdate();

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                ShowPassage(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ShowPassage(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ShowPassage(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ShowPassage(3);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ShowPassage(4);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                ShowPassage(5);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                ShowPassage(6);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                ShowPassage(7);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                ShowPassage(8);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                ShowPassage(9);
            }
        }

        #endregion


        #region Events on Passage

        public void CubeAnimationStateChanged(System.Object[] args)
        {
            if(Cube.AnimationState == CubeAnimationManager.CubeAnimationState.FOLDING || Cube.AnimationState == CubeAnimationManager.CubeAnimationState.IDLE_AS_FOLDED)
            {
                ShowPassage(-1);
            }
        }
        
        public void ReleasedFinger(System.Object[] args)
        {
            //Log.Status("PassageAnimationManager", "ReleasedFinger");
            if (m_IsTouchOnDragging)
            {
                m_IsTouchOnDragging = false;
                FingerReleasedAfterDragging();
            }
           
        }

        public void TapOnCubeSide(System.Object[] args)
        {
            if (args != null && args.Length == 2 && args[0] is TouchScript.Gestures.TapGesture && args[1] is RaycastHit)
            {
                if (Cube.AnimationState == CubeAnimationManager.CubeAnimationState.IDLE_AS_FOCUSED) //TODO: Delete these true condition - it is for test purpose
                {
                    
                    TouchScript.Gestures.TapGesture tapGesture = args[0] as TouchScript.Gestures.TapGesture;

                    /*
                    Ray rayForDrag = UnityEngine.Camera.main.ScreenPointToRay(tapGesture.ScreenPosition);
                    RaycastHit2D hit;
                    hit = Physics2D.Raycast(rayForDrag.origin, rayForDrag.direction, Mathf.Infinity, 1 << this.gameObject.layer);

                    startPoint = rayForDrag.origin;
                    endPoint = rayForDrag.origin + 200 * rayForDrag.direction;
                    if (hit.collider != null)
                    {
                        Log.Status("PassageAnimationManager", "TapOnCubeSide - HIT: " + hit.transform.name + " - Parent: " + hit.transform.parent.name);
                    }
                    else
                    {
                        Log.Status("PassageAnimationManager", "TapOnCubeSide - Not Hit");
                        //do not hit any passage
                    }
                    */
                    if(EventSystem.current != null)
                    {
                        // get pointer event data, then set current mouse position
                        PointerEventData ped = new PointerEventData(EventSystem.current);
                        ped.position = tapGesture.ScreenPosition;
                        List<RaycastResult> hits = new List<RaycastResult>();
                        EventSystem.current.RaycastAll(ped, hits);

                        RaycastResult hitResult = default(RaycastResult);
                        bool hitOnPanel = false;
                        int panelIndexToShow = -1;

                        string namePanel = "Panel";
                        string nameTabItem = "TabItem";
                        string nameTabImage = "Tab";
                        string namePrefixPassageItem = "PassageItem_";

                        foreach (RaycastResult r in hits)       
                        {
                            if (r.gameObject.layer == this.gameObject.layer && (string.Equals(r.gameObject.name, namePanel) || string.Equals(r.gameObject.name, nameTabItem) || string.Equals(r.gameObject.name, nameTabImage)))
                            {
                                Log.Status("PassageAnimationManager", "RaycastResult - r: " + r.gameObject.name + " PArent: " + r.gameObject.transform.parent.name);
                                hitOnPanel = true;
                                hitResult = r;

                                if (string.Equals(r.gameObject.name, nameTabImage))
                                    int.TryParse(hitResult.gameObject.transform.parent.parent.name.Substring(namePrefixPassageItem.Length, 2), out panelIndexToShow);
                                else
                                    int.TryParse(hitResult.gameObject.transform.parent.name.Substring(namePrefixPassageItem.Length, 2), out panelIndexToShow);
                                
                                break;
                            }
                            
                        }

                        if (hitOnPanel && panelIndexToShow >=0)
                        {
                            Log.Status("PassageAnimationManager", "TapOnCubeSide - HIT: " + hitResult.gameObject.name + " - Parent: " + hitResult.gameObject.transform.parent.name);
                            ShowPassage(panelIndexToShow);

                        }
                        else
                        {
                            Log.Status("PassageAnimationManager", "TapOnCubeSide - Not Hit");
                        }
                    }
                    else
                    {
                        Log.Warning("PassageAnimationManager", "EventSystem couldn't find in the current scene");
                    }
                    

                    //RaycastHit raycastHit = (RaycastHit)args[1];

                    //Cube.OnTapInside(tapGesture, raycastHit);
                }
                else
                {
                    //do nothing - we are not instersted the taps in other states
                }
            }
            else
            {
                Log.Warning("PassageAnimationManager", "TapOnCubeSide has invalid arguments!");
            }
        }

        Vector3 startPoint;
        Vector3 endPoint;
        void OnDrawGizmos()
        {

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint, endPoint);
        }

        public void OneFingerDragOnCube(System.Object[] args)
        {
           // Log.Status("PassageAnimationManager", "OneFingerDragOnCube");
            if (args != null && args.Length == 1 && args[0] is TouchScript.Gestures.ScreenTransformGesture)
            {
                TouchScript.Gestures.ScreenTransformGesture OneFingerManipulationGesture = args[0] as TouchScript.Gestures.ScreenTransformGesture;

                if (Cube.AnimationState == CubeAnimationManager.CubeAnimationState.IDLE_AS_FOCUSED) //TODO: Delete this true condition! it is for test purposes!
                {
                    DragOneFingerOnFocusedSide(OneFingerManipulationGesture);
                }
                else
                {
                    //do nothing because cube is not in Focused state!
                }
            }
            else
            {
                Log.Warning("QuestWidget", "OneFingerDragOnCube has invalid arguments");
            }
            
        }

        public void OneFingerDragFullScreen(System.Object[] args)
        {
            //Log.Status("PassageAnimationManager", "OneFingerDragFullScreen");
            if (m_IsTouchOnDragging)
            {
                m_IsTouchOnDragging = false;
                FingerReleasedAfterDragging();
            }
           
        }

       

        #endregion

        #region Passage Path Update
        
        public void UpdatePassages()
        {
            m_PassageItems = Utility.FindObjects<Transform>(this.gameObject, "PassageItem", isContains: true, sortByName: true);
            Log.Status("PassageAnimationManager", "Updated Passages with number of {0} passages", NumberOfPassages);

            UpdateBezierPathForPassages();
            m_AnimationLocationRatio = new float[NumberOfPassages];
            m_AnimationRotationRatio = new float[NumberOfPassages];
            m_TargetLocation = new Vector3[NumberOfPassages];
            m_TargetRotation = new Vector3[NumberOfPassages];
            for (int i = 0; i < NumberOfPassages; i++)
            {
                m_AnimationLocationRatio[i] = 0.0f;
                m_AnimationRotationRatio[i] = 0.0f;
                m_TargetLocation[i] = m_BezierPathToCenter[i].pts[0];   // PassageList[i].localPosition;
                m_TargetRotation[i] = m_BezierPathOrientationToCenter[i].pts[0];   //PassageList[i].localEulerAngles;
            }
            
        }

        void UpdateBezierPathForPassages()
        {
            if(m_PassageItems != null && m_PassageItems.Length > 0)
            {
                m_BezierPathToCenter = new LTBezierPath[NumberOfPassages];
                m_BezierPathOrientationToCenter = new LTBezierPath[NumberOfPassages];
                m_BezierPathToStack = new LTBezierPath[NumberOfPassages];
                m_BezierPathOrientationToStack = new LTBezierPath[NumberOfPassages];
                m_BezierPathFromInitialToStack = new LTBezierPath[NumberOfPassages];
                m_BezierPathOrientationFromInitialToStack = new LTBezierPath[NumberOfPassages];

                for (int i = 0; i < NumberOfPassages; i++)
                {
                    m_BezierPathToCenter[i] = new LTBezierPath(new Vector3[]{
                        m_PathToCenterForFirstItem[0] + (m_OffsetPathToCenter * i) + (m_OffsetPathToCenter * (10 - NumberOfPassages)),
                        m_PathToCenterForFirstItem[1] + (m_OffsetPathToCenter * i) + (m_OffsetPathToCenter * (10 - NumberOfPassages)),
                        m_PathToCenterForFirstItem[2] ,
                        m_PathToCenterForFirstItem[3] });

                    m_BezierPathOrientationToCenter[i] = new LTBezierPath(new Vector3[]{
                        m_PathOrientationToCenterForFirstItem[0] + (m_OffsetPathOrientationToCenter * i),
                        m_PathOrientationToCenterForFirstItem[1] + (m_OffsetPathOrientationToCenter * i),
                        m_PathOrientationToCenterForFirstItem[2],
                        m_PathOrientationToCenterForFirstItem[3]});

                    m_BezierPathToStack[i] = new LTBezierPath(new Vector3[]{
                        m_PathToStackForFirstItem[0] ,
                        m_PathToStackForFirstItem[1] ,
                        m_PathToStackForFirstItem[2] + (m_OffsetPathToStack * i),
                        m_PathToStackForFirstItem[3] + (m_OffsetPathToStack * i)});

                    m_BezierPathOrientationToStack[i] = new LTBezierPath(new Vector3[]{
                        m_PathOrientationToStackForFirstItem[0],
                        m_PathOrientationToStackForFirstItem[1],
                        m_PathOrientationToStackForFirstItem[2] + (m_OffsetPathOrientationToStack * i),
                        m_PathOrientationToStackForFirstItem[3] + (m_OffsetPathOrientationToStack * i)});

                    m_BezierPathFromInitialToStack[i] = new LTBezierPath(new Vector3[]{
                        m_BezierPathToCenter[i].pts[0],
                        m_BezierPathToCenter[i].pts[1],
                        m_BezierPathToStack[i].pts[2],
                        m_BezierPathToStack[i].pts[3]});

                    m_BezierPathOrientationFromInitialToStack[i] = new LTBezierPath(new Vector3[]{
                        m_BezierPathOrientationToCenter[i].pts[0],
                        m_BezierPathOrientationToCenter[i].pts[1],
                        m_BezierPathOrientationToStack[i].pts[2],
                        m_BezierPathOrientationToStack[i].pts[3]});
                }
            }
            
        }
        #endregion

        

        //[SerializeField]
        private float m_OneDragModifier = 0.002f;
        private int m_SelectedPassageIndex = -1;
        

        public void DragOneFingerOnFocusedSide(TouchScript.Gestures.ScreenTransformGesture OneFingerManipulationGesture)
        {

            if (m_PassageItems == null)
            {
                m_PassageItems = Utility.FindObjects<Transform>(this.gameObject, "PassageItem", isContains: true, sortByName: true);
            }

            if (m_PassageItems != null)
            {
                float movingInX = OneFingerManipulationGesture.DeltaPosition.x * m_OneDragModifier;

                m_IsTouchOnDragging = true;

                if (m_SelectedPassageIndex < 0)
                {
                    m_SelectedPassageIndex = 0;
                }
                else if(m_SelectedPassageIndex >= NumberOfPassages)
                {
                    m_SelectedPassageIndex = NumberOfPassages - 1;
                }
                //else
                //{
                    //Ray rayForDrag = UnityEngine.Camera.main.ScreenPointToRay(OneFingerManipulationGesture.ScreenPosition);
                    //RaycastHit hit;
                    //bool isHitOnFocusedSide = Physics.Raycast(rayForDrag, out hit, Mathf.Infinity, 1 << this.transform.parent.gameObject.layer);

                    //if (isHitOnFocusedSide)
                    //{
                    //    int touchedSide = -1;
                    //    int.TryParse(hit.transform.name.Substring(1, 1), out touchedSide);
                    //    CubeSideType cubeSideTouched = (CubeSideType)touchedSide;

                    //    Log.Status("CubeAnimationManager", "cubeSideTouched: {0}", cubeSideTouched);
                    //    if (cubeSideTouched == CubeSideType.TITLE && SideFocused == CubeSideType.TITLE)
                    //    {
                    //        m_LastFrameOneFingerDrag = Time.frameCount;
                    //        //DragOneFingerOnPassage(OneFingerManipulationGesture);
                    //    }
                    //}
                    
                    

                    m_AnimationLocationRatio[m_SelectedPassageIndex] = Mathf.Clamp01(m_AnimationLocationRatio[m_SelectedPassageIndex] + movingInX);
                    m_AnimationRotationRatio[m_SelectedPassageIndex] = Mathf.Clamp01(m_AnimationRotationRatio[m_SelectedPassageIndex] + movingInX);

                    SetTargetLocationAndRotationOfSelectedPassage();

                //}


            }
            else
            {
                Log.Status("CubeAnimationManager", "NO PASSAGE - DragOneFingerOnPassage: {0}", OneFingerManipulationGesture.DeltaPosition);
            }
        }

        private void SetTargetLocationAndRotationOfSelectedPassage()
        {
            if (m_SelectedPassageIndex < 0 || m_SelectedPassageIndex >= NumberOfPassages)
                return;

            LTBezierPath m_BezierPathCurrent;
            LTBezierPath m_BezierPathOrientationCurrent;

            float m_RatioBezierPathPassage = 0.0f;
            if (m_AnimationLocationRatio[m_SelectedPassageIndex] <= 0.5f)
            {
                m_RatioBezierPathPassage = m_AnimationLocationRatio[m_SelectedPassageIndex] * 2.0f;
                m_BezierPathCurrent = m_BezierPathToCenter[m_SelectedPassageIndex];
                m_BezierPathOrientationCurrent = m_BezierPathOrientationToCenter[m_SelectedPassageIndex];
            }
            else
            {
                m_RatioBezierPathPassage = (m_AnimationLocationRatio[m_SelectedPassageIndex] - 0.5f) * 2.0f;
                m_BezierPathCurrent = m_BezierPathToStack[m_SelectedPassageIndex];
                m_BezierPathOrientationCurrent = m_BezierPathOrientationToStack[m_SelectedPassageIndex];
            }

            m_TargetLocation[m_SelectedPassageIndex] = m_BezierPathCurrent.point(m_RatioBezierPathPassage);
            m_TargetRotation[m_SelectedPassageIndex] = m_BezierPathOrientationCurrent.point(m_RatioBezierPathPassage);

        }

        private void FingerReleasedAfterDragging()
        {
            if (!m_IsTouchOnDragging && m_SelectedPassageIndex >= 0)
            {
                if (m_AnimationLocationRatio[m_SelectedPassageIndex] < m_PercentToGoInitialPosition)
                {
                    ShowPassage(m_SelectedPassageIndex - 1);
                    //m_AnimationLocationRatio[m_SelectedPassageIndex] = 0.0f;
                }
                else if (m_AnimationLocationRatio[m_SelectedPassageIndex] > m_PercentToGoStackPosition)
                {
                    ShowPassage(m_SelectedPassageIndex + 1);
                    //m_AnimationLocationRatio[m_SelectedPassageIndex] = 1.0f;
                }
                else
                {
                    //ShowPassage(m_SelectedPassageIndex);
                    //m_AnimationLocationRatio[m_SelectedPassageIndex] = 0.5f;
                    m_AnimationLocationRatio[m_SelectedPassageIndex] = 0.5f;
                    m_AnimationRotationRatio[m_SelectedPassageIndex] = 0.5f;
                    SetTargetLocationAndRotationOfSelectedPassage();
                    // SetTargetLocationAndRotationOfSelectedPassage();
                }
                
                //m_SelectedPassageIndex = -1;
            }
        }

        //[SerializeField]
        private float m_SpeedPassageAnimation = 4.0f;
        private float m_PercentToGoInitialPosition = 0.26f;
        private float m_PercentToGoStackPosition = 0.65f;
        private void DragOneFingerOnPassageOnUpdate()
        {
            if (m_PassageItems != null)
            {
                for (int i = 0; i < m_PassageItems.Length; i++)
                {
                    if(m_PassageItems[i] != null && m_PassageItems[i].transform != null)
                    {
                        m_PassageItems[i].transform.localPosition = Vector3.Lerp(m_PassageItems[i].transform.localPosition, m_TargetLocation[i], Time.deltaTime * m_SpeedPassageAnimation);
                        m_PassageItems[i].transform.localRotation = Quaternion.Lerp(m_PassageItems[i].transform.localRotation, Quaternion.Euler(m_TargetRotation[i]), Time.deltaTime * m_SpeedPassageAnimation);
                    }
                }
            }
        }

        private LTBezierPath getBezierPathFromInitialValue(Vector3[] currentPath, Vector3 initialValue, float percent = 0.2f)
        {

            return new LTBezierPath(new Vector3[] {
                    initialValue,
                    Vector3.Lerp(initialValue, currentPath[3], percent),
                    Vector3.Lerp(initialValue, currentPath[3], 1.0f - percent),
                    currentPath[3]
            });
        }

        private LTBezierPath getBezierPathToLastValue(Vector3[] currentPath, Vector3 lastValue, float percent = 0.2f)
        {

            return new LTBezierPath(new Vector3[] {
                    currentPath[0],
                    Vector3.Lerp(currentPath[0], lastValue, percent),
                    Vector3.Lerp(currentPath[0], lastValue, 1.0f - percent),
                    lastValue
            });
        }

        private LTDescr[] m_AnimationToShowPositionPassage;
        private LTDescr[] m_AnimationToShowRotationPassage;
        private int m_PreviousPassageIndex = 0;
        private void ShowPassage(int passageIndexToShow)
        {

            m_PreviousPassageIndex = m_SelectedPassageIndex;
            m_SelectedPassageIndex = passageIndexToShow;
            // UnityEngine.Debug.Break();
            Log.Status("PassageAnimationManager", "ShowPassage : {0}, PreviousOne: {1}", passageIndexToShow, m_PreviousPassageIndex);

            StopAnimations();

            if (m_AnimationToShowPositionPassage == null || m_AnimationToShowPositionPassage.Length != NumberOfPassages)
                m_AnimationToShowPositionPassage = new LTDescr[NumberOfPassages];

            if (m_AnimationToShowRotationPassage == null || m_AnimationToShowRotationPassage.Length != NumberOfPassages)
                m_AnimationToShowRotationPassage = new LTDescr[NumberOfPassages];

            float animationTime = 1.0f;
            float delayOnPassage = 0.07f;
            float delayExtraOnMainPassage = 0.07f;
            LeanTweenType leanType = LeanTweenType.easeOutCirc;

            for (int i = 0; i < NumberOfPassages; i++)
            {
                if (PassageList[i] == null || PassageList[i].transform == null)
                {
                    Log.Warning("PassageAnimationManager", "PassageList doesn't have the element index: {0}", i);
                    continue;
                }
                else
                {
                    //Going to initial position if they are in different position
                    if (i > passageIndexToShow)
                    {
                        LTBezierPath pathFromCurrentPosition = getBezierPathToLastValue(m_BezierPathFromInitialToStack[i].pts, m_TargetLocation[i]);
                        //LTBezierPath pathFromCurrentRotation = getBezierPathToLastValue(m_BezierPathOrientationFromInitialToStack[i].pts, PassageList[i].localEulerAngles);
                        //AnimatePassageToGivenRatio(animationTime, delayOnPassage * Mathf.Abs(m_PreviousPassageIndex - i), leanType, i, m_AnimationLocationRatio[i], 0.0f, pathFromCurrentPosition, pathFromCurrentRotation);

                        AnimatePassageToGivenRatio(animationTime, delayOnPassage * Mathf.Abs(m_PreviousPassageIndex - i), leanType, i, m_AnimationLocationRatio[i], 0.0f, pathFromCurrentPosition, m_BezierPathOrientationFromInitialToStack[i]);

                        PassageList[i].SetSiblingIndex(NumberOfPassages - 1 - i);
                    }
                    else if (i < passageIndexToShow)
                    {
                        LTBezierPath pathFromCurrentPosition = getBezierPathFromInitialValue(m_BezierPathFromInitialToStack[i].pts, m_TargetLocation[i]);
                        //LTBezierPath pathFromCurrentRotation = getBezierPathFromInitialValue(m_BezierPathOrientationFromInitialToStack[i].pts, PassageList[i].localEulerAngles);

                        AnimatePassageToGivenRatio(animationTime, delayOnPassage * Mathf.Abs(m_PreviousPassageIndex - i), leanType, i, m_AnimationLocationRatio[i], 1.0f, pathFromCurrentPosition, m_BezierPathOrientationFromInitialToStack[i]);

                        PassageList[i].SetSiblingIndex(NumberOfPassages - 1 - i);
                    }
                    else
                    {
                        if (m_PreviousPassageIndex > passageIndexToShow)
                            PassageList[i].SetSiblingIndex(NumberOfPassages - 1 - i);
                        //PassageList[i].SetSiblingIndex(NumberOfPassages);

                        LTBezierPath pathToMove = m_AnimationLocationRatio[i] <= 0.5f ? m_BezierPathToCenter[i] : m_BezierPathToStack[i];
                        LTBezierPath pathToRotate = m_AnimationRotationRatio[i] <= 0.5f ? m_BezierPathOrientationToCenter[i] : m_BezierPathOrientationToStack[i];
                        float targetRatio = m_AnimationLocationRatio[i] <= 0.5f ? 1.0f : 0.0f;
                        float currentRatio = m_AnimationLocationRatio[i] <= 0.5f ? 0.0f : 1.0f; ; // m_AnimationLocationRatio[i] <= 0.5f ? (m_AnimationLocationRatio[i] * 2.0f) : ((m_AnimationLocationRatio[i] - 0.5f) * 2.0f);

                        if (targetRatio == 1.0f)
                        {
                            pathToMove = getBezierPathFromInitialValue(pathToMove.pts, PassageList[i].localPosition);
                            pathToRotate = getBezierPathFromInitialValue(pathToRotate.pts, new Vector3(PassageList[i].localEulerAngles.x, PassageList[i].localEulerAngles.y, 0.0f));
                        }
                        else
                        {
                            pathToMove = getBezierPathToLastValue(pathToMove.pts, PassageList[i].localPosition);
                            pathToRotate = getBezierPathToLastValue(pathToRotate.pts, new Vector3(PassageList[i].localEulerAngles.x, PassageList[i].localEulerAngles.y, 0.0f));
                        }

                        //PassageList[i].SetAsLastSibling();
                        AnimatePassageToGivenRatio(animationTime, (delayOnPassage * Mathf.Abs(m_PreviousPassageIndex - i)) + delayExtraOnMainPassage, leanType, i, currentRatio, targetRatio, pathToMove, pathToRotate, isUsingTwoAnimations: true);
                    }
                    //m_AnimationToShowRotationPassage[i] =

                    //m_PassageItems[i].transform.localPosition = Vector3.Lerp(m_PassageItems[i].transform.localPosition, m_BezierPathToCenter[i].point(0.0f), Time.deltaTime * m_SpeedPassageAnimation);
                    //m_PassageItems[i].transform.localRotation = Quaternion.Lerp(m_PassageItems[i].transform.localRotation, Quaternion.Euler(m_BezierPathOrientationToCenter[i].point(0.0f)), Time.deltaTime * m_SpeedPassageAnimation);

                }
            }
            
        }


        
        private void AnimatePassageToGivenRatio(float animationTime, float delayOnPassage, LeanTweenType leanType, int passageIndex, float currentRatio, float targetRatio, LTBezierPath bezierPathToMove, LTBezierPath bezierPathToRotate, bool isUsingTwoAnimations = false)
        {

            float timeModifier = Mathf.Abs(targetRatio - currentRatio);

            if (m_AnimationLocationRatio[passageIndex] != targetRatio || m_AnimationRotationRatio[passageIndex] != targetRatio)
            {

                //Log.Status("PassageAnimationManager", "AnimatePassageToGivenRatio -passageIndex: {0}, PassageList.Length:{1}", passageIndex, PassageList.Length);

                bool hasChangeSiblingIndex = false;
                m_AnimationToShowPositionPassage[passageIndex] = LeanTween.value(PassageList[passageIndex].gameObject, currentRatio, targetRatio, animationTime * timeModifier).setDelay(delayOnPassage).setEase(leanType).setOnUpdate(
                (float f) =>
                {
                    //Log.Status("PassageAnimationManager", "m_TargetLocation.length: {0} , passageIndex: {1}, m_AnimationLocationRatio.Length:{2}", m_TargetLocation.Length, passageIndex, m_AnimationLocationRatio.Length);
                    //PassageList[passageIndex].localPosition = bezierPathToMove.pointNotNAN(f);
                    m_TargetLocation[passageIndex] = bezierPathToMove.pointNotNAN(f);
                    if (isUsingTwoAnimations)
                    {
                        if (targetRatio == 1.0f)    //this is when passage goes from initial to center
                        {
                            m_AnimationLocationRatio[passageIndex] = f / 2.0f;
                        }
                        else if( targetRatio == 0.0f)
                        {
                            m_AnimationLocationRatio[passageIndex] = f / 2.0f + 0.5f;
                        }
                        else
                        {
                            Log.Warning("PassageAnimationMaanger", "Unknown Target ratio to animate Passages");
                        }
                    }
                    else
                    {
                        m_AnimationLocationRatio[passageIndex] = f;
                    }

                    if (Mathf.Abs(f - targetRatio) < 0.05f && !hasChangeSiblingIndex)
                    {
                        hasChangeSiblingIndex = true;
                        if (isUsingTwoAnimations)
                            PassageList[passageIndex].SetSiblingIndex(NumberOfPassages);
                        else
                            PassageList[passageIndex].SetSiblingIndex(NumberOfPassages - 1 - passageIndex);
                    }

                }).setOnComplete(()=> {
                    //if (isUsingTwoAnimations)
                    //    PassageList[passageIndex].SetSiblingIndex(NumberOfPassages);
                    //else
                    //    PassageList[passageIndex].SetSiblingIndex(NumberOfPassages - 1 - passageIndex);
                });
                
            }
            else
            {
                //no ned to create movement animation - passage is already in initial position.
            }

            if (m_AnimationLocationRatio[passageIndex] != targetRatio || m_AnimationRotationRatio[passageIndex] != targetRatio)
            {
                m_AnimationToShowRotationPassage[passageIndex] = LeanTween.value(PassageList[passageIndex].gameObject, currentRatio, targetRatio, animationTime * timeModifier).setDelay(delayOnPassage).setEase(leanType).setOnUpdate(
                    (float f) =>
                    {
                        //Log.Status("PassageAnimationManager", "Rotation : {0} at {1}  - pts: {2}-{3}-{4}-{5} ", bezierPathToRotate.pointNotNAN(f), f, bezierPathToRotate.pts[0], bezierPathToRotate.pts[1], bezierPathToRotate.pts[2], bezierPathToRotate.pts[3]);
                        //PassageList[passageIndex].localEulerAngles = bezierPathToRotate.pointNotNAN(f);
                        m_TargetRotation[passageIndex] = bezierPathToRotate.pointNotNAN(f);

                        if (isUsingTwoAnimations)
                        {
                            if (targetRatio == 1.0f)    //this is when passage goes from initial to center
                            {
                                m_AnimationRotationRatio[passageIndex] = f / 2.0f;
                            }
                            else if (targetRatio == 0.0f)
                            {
                                m_AnimationRotationRatio[passageIndex] = f / 2.0f + 0.5f;
                            }
                            else
                            {
                                Log.Warning("PassageAnimationMaanger", "Unknown Target ratio to animate Passages");
                            }
                        }
                        else
                        {
                            m_AnimationRotationRatio[passageIndex] = f;
                        }
                        
                    });
            }
            else
            {
                //No need to create rotation animation - passage is already in initial rotation.
            }
        }

        private void StopAnimations()
        {
            if(m_AnimationToShowPositionPassage != null)
            {
                for (int i = 0; i < m_AnimationToShowPositionPassage.Length; i++)
                {
                    if (m_AnimationToShowPositionPassage[i] != null)
                    {
                        m_AnimationToShowPositionPassage[i].hasUpdateCallback = false;
                        LeanTween.cancel(m_AnimationToShowPositionPassage[i].uniqueId);
                        //m_AnimationToShowPositionPassage[i] = null;
                    }
                    else
                    {
                        // Log.Warning("PassageAnimationManager", "There is no m_AnimationToShowPositionPassage defined for animation: {0} ", i);
                    }

                }
            }

            if (m_AnimationToShowRotationPassage != null)
            {
                for (int i = 0; i < m_AnimationToShowRotationPassage.Length; i++)
                {
                    if (m_AnimationToShowRotationPassage[i] != null)
                    {
                        m_AnimationToShowRotationPassage[i].hasUpdateCallback = false;
                        LeanTween.cancel(m_AnimationToShowRotationPassage[i].uniqueId);
                       // m_AnimationToShowRotationPassage[i] = null;
                    }
                    else
                    {
                        //  Log.Warning("PassageAnimationManager", "There is no m_AnimationToShowRotationPassage defined for animation: {0} ", i);
                    }

                }
            }
        }
        
    }
    
}