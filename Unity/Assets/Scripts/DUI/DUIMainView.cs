﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;

public class DUIMainView : DUIView
{
    public enum ENavButDirection
    {
        Horizontal,
        Vertical
    }

    private Dictionary<string, DUISubView> m_subViews;
    
    public Rect m_titleRect             { get; set; }
    public Rect m_navAreaRect           { get; set; }
    public Rect m_subViewAreaRect       { get; set; }

    private EQuality m_quality;
    private ENavButDirection m_navButtonDirection;
    
    private Camera m_renderCamera;
    private RenderTexture m_renderTex;

    // Member Methods
    public void Initialise(TextAsset _uiXmlDoc)
    {
        // Load the XML file for the UI and save the base node for the ui
        m_uiXmlNode = LoadXML(_uiXmlDoc).SelectSingleNode("mainview");

        // Get the screen details
        XmlNode screenNode = m_uiXmlNode.SelectSingleNode("screen");
        m_quality = (EQuality)System.Enum.Parse(typeof(EQuality), screenNode.Attributes["quality"].Value);
        m_dimensions = StringToVector2(screenNode.Attributes["dimensions"].Value);

        // Setup the main view
        SetupMainView();

        // Setup the render texture
        SetupRenderTex();

        // Setup the render camera
        SetupRenderCamera();
    }

    public void AttatchRenderTexture(Material _sharedScreenMat)
    {
        // Set the render text onto the material of the screen
        _sharedScreenMat.SetTexture("_MainTex", m_renderTex); 
    }

    public void AddSubview(string _subViewName)
    {
        TextAsset ta = (TextAsset)Resources.Load("XMLs/DUI/subviews/" + _subViewName);

        // Create the DUI game object
        GameObject duiGo = new GameObject();
        duiGo.transform.parent = transform;
        duiGo.name = name + "_SubView_" + _subViewName;
        duiGo.layer = gameObject.layer;
        duiGo.transform.localRotation = Quaternion.identity;
        
        // Add the DUI component
        DUISubView DUISV = duiGo.AddComponent<DUISubView>();

        // Initialise the DUI Component
        DUISV.Initialise(ta, new Vector2(m_subViewAreaRect.width * m_dimensions.x, m_subViewAreaRect.height * m_dimensions.y));

        // Register the button for the event
        DUISV.m_navButton.eventPress += NavigationButtonPressed;

        // Deavtivate the game object
        DUISV.gameObject.SetActive(false);

        // Add to the dictionary
        m_subViews[_subViewName] = DUISV;

        // Reposition the buttons
        RepositionButtons();
    }

    private void Awake()
    {
        m_subViews = new Dictionary<string, DUISubView>();
    }

    private void Update()
    {
        DebugRenderRects();

        // Update the render texture and camera
        m_renderTex.DiscardContents(true, true);
        RenderTexture.active = m_renderTex;

        m_renderCamera.Render();

        RenderTexture.active = null;
    }

    private void SetupMainView()
    {
        // Get the Title info
        m_titleRect = DUIMainView.StringToRect(m_uiXmlNode.SelectSingleNode("title").Attributes["rect"].Value);

        // Get the Navigation Area info
        XmlNode navViewNode = m_uiXmlNode.SelectSingleNode("navarea");
        m_navAreaRect = DUIMainView.StringToRect(navViewNode.Attributes["rect"].Value);
        m_navButtonDirection = (ENavButDirection)System.Enum.Parse(typeof(ENavButDirection), navViewNode.Attributes["butdir"].Value);

        // Get the Subview Area info
        m_subViewAreaRect = DUIMainView.StringToRect(m_uiXmlNode.SelectSingleNode("subviewarea").Attributes["rect"].Value);

        // Setup the title
        SetupTitle();
    }

    private void SetupTitle()
    {
        // Get the title node
        XmlNode titleNode = m_uiXmlNode.SelectSingleNode("title");

        string text = titleNode.Attributes["text"].Value;
        Vector3 localPos = new Vector3(m_titleRect.center.x * m_dimensions.x - (m_dimensions.x * 0.5f),
                                      m_titleRect.center.y  * m_dimensions.y - (m_dimensions.y * 0.5f));

        // Create the title
        CreateTitle(text, localPos);
    }

    private void SetupRenderTex()
    {
        // Figure out the pixels per meter for the screen based on quality setting
        float ppm = 0.0f;
        switch (m_quality)
        {
            case EQuality.VeryHigh: ppm = 500; break;
            case EQuality.High: ppm = 400; break;
            case EQuality.Medium: ppm = 300; break;
            case EQuality.Low: ppm = 200; break;
            case EQuality.VeryLow: ppm = 100; break;
            
            default:break;
        }

        int width = (int)(m_dimensions.x * ppm);
        int height = (int)(m_dimensions.y * ppm);

        // Create a new render texture
        m_renderTex = new RenderTexture(width, height, 16);
        m_renderTex.name = name + " RT";
        m_renderTex.Create();
    }

    private void SetupRenderCamera()
    {
        // Create the camera game object
        GameObject go = new GameObject();
        go.name = transform.name + "_RenderCamera";
        go.transform.parent = transform;
        go.transform.localPosition = new Vector3(0.0f, 0.0f, -1.0f);
        go.transform.localRotation = Quaternion.identity;
        go.layer = LayerMask.NameToLayer("DUI");

        // Get the render camera and set its target as the render texture
        m_renderCamera = go.AddComponent<Camera>();
        m_renderCamera.cullingMask = 1 << LayerMask.NameToLayer("DUI");
        m_renderCamera.orthographic = true;
        m_renderCamera.backgroundColor = Color.black;
        m_renderCamera.nearClipPlane = 0.0f;
        m_renderCamera.farClipPlane = 2.0f;
        m_renderCamera.targetTexture = m_renderTex;
        m_renderCamera.orthographicSize = m_dimensions.y * 0.5f;
    }

    private void CreateTitle(string _text, Vector3 _localPos)
    {
        GameObject titleGo = new GameObject();

        // Set the default values
        titleGo.name = name + "_Title";
        titleGo.layer = gameObject.layer;
        titleGo.transform.parent = transform;
        titleGo.transform.localPosition = _localPos;
        titleGo.transform.localRotation = Quaternion.identity;

        MeshRenderer mr = titleGo.AddComponent<MeshRenderer>();
        mr.material = (Material)Resources.Load("Fonts/Arial", typeof(Material));

        TextMesh tm = titleGo.AddComponent<TextMesh>();
        tm.fontSize = 0;
        tm.characterSize = 0.05f;
        tm.font = (Font)Resources.Load("Fonts/Arial", typeof(Font));
        tm.anchor = TextAnchor.MiddleCenter;
        tm.text = _text;
    }

    private void ShowSubView(DUISubView _subView)
    {
        // Place it in the middle
        float x = m_subViewAreaRect.center.x * m_dimensions.x - (m_dimensions.x * 0.5f);
        float y = m_subViewAreaRect.center.y * m_dimensions.y - (m_dimensions.y * 0.5f);
        _subView.transform.position = new Vector3(x, y) + transform.position;

        // Make it active
        _subView.gameObject.SetActive(true);
    }

    private void RepositionButtons()
    {
        int numSubViews = m_subViews.Count;
        int count = 0;

        foreach (DUISubView subView in m_subViews.Values)
        {
            DUIButton navButton = subView.m_navButton;
           
            // Calculate the position for the nav button to go
            if (m_navButtonDirection == ENavButDirection.Vertical)
            {
                float x = m_navAreaRect.center.x * m_dimensions.x - (m_dimensions.x * 0.5f);
                float y = ((m_navAreaRect.yMax - m_navAreaRect.y) * (count + 0.5f) / numSubViews) * m_dimensions.y - (m_dimensions.y * 0.5f);

                navButton.transform.localPosition = new Vector3(x, y);
            }
            else if (m_navButtonDirection == ENavButDirection.Horizontal)
            {
                float x = (m_navAreaRect.xMax - m_navAreaRect.x) * (count + 0.5f) / numSubViews * m_dimensions.x - (m_dimensions.x * 0.5f);
                float y = m_navAreaRect.center.y * m_dimensions.y - (m_dimensions.y * 0.5f);

                navButton.transform.localPosition = new Vector3(x, y);
            }

            count += 1;
        }
    }

    public void CheckDGUICollisions(RaycastHit _rh)
    {
        Vector3 offset = new Vector3(_rh.textureCoord.x * m_dimensions.x - m_dimensions.x * 0.5f,
                                     _rh.textureCoord.y * m_dimensions.y - m_dimensions.y * 0.5f,
                                             0.0f);

        offset = transform.rotation * offset;
        Vector3 rayOrigin = transform.position + offset + transform.forward * -1.0f;

        Ray ray = new Ray(rayOrigin, transform.forward);
        RaycastHit hit;
        float rayLength = 2.0f;

        if (Physics.Raycast(ray, out hit, rayLength, 1 << LayerMask.NameToLayer("DUI")))
        {
            DUIButton bUI = hit.transform.parent.gameObject.GetComponent<DUIButton>();
            if (bUI)
            {
                Debug.Log("Button Hit: " + hit.transform.parent.name);
                bUI.OnPress();
            }

            Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.green, 0.5f);
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.red, 0.5f);
        }
    }

    // Event handler functions
    private void NavigationButtonPressed(object _sender, EventArgs _eventArgs)
    {
        foreach(DUISubView subView in m_subViews.Values)
        {
            DUIButton button = subView.m_navButton;

            if (button == _sender)
            {
                // Create the subview
                ShowSubView(subView);
                break;
            }
        }
    }

    // Debug functions
    private void DebugRenderRects()
    {
        // Test for rendering title, nav and content areas
        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.zero;

        start = new Vector3(-(m_dimensions.x * 0.5f), -(m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3((m_dimensions.x * 0.5f), -(m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end, Color.green);

        start = new Vector3(-(m_dimensions.x * 0.5f), -(m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(-(m_dimensions.x * 0.5f), (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end, Color.green);

        start = new Vector3((m_dimensions.x * 0.5f), (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3((m_dimensions.x * 0.5f), -(m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end, Color.green);

        start = new Vector3((m_dimensions.x * 0.5f), (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(-(m_dimensions.x * 0.5f), (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end, Color.green);




        start = new Vector3(m_titleRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_titleRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_titleRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_titleRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_titleRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_titleRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_titleRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_titleRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_titleRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);






        start = new Vector3(m_navAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_navAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_navAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_navAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_navAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_navAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_navAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_navAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_navAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);





        start = new Vector3(m_subViewAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_subViewAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_subViewAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_subViewAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_subViewAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_subViewAreaRect.x * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);

        start = new Vector3(m_subViewAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.yMax * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;
        end = new Vector3(m_subViewAreaRect.xMax * m_dimensions.x - (m_dimensions.x * 0.5f), m_subViewAreaRect.y * m_dimensions.y - (m_dimensions.y * 0.5f)) + transform.position;

        Debug.DrawLine(start, end);
    }
}
