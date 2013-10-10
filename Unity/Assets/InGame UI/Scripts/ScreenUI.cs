﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;

public class ScreenUI : MonoBehaviour
{
    // Member Variables
    public RenderTexture m_RenderTex    { get; set; }
    public Camera m_RenderCamera        { get; set; }
    public GameObject m_UI              { get; set; }

    private ScreenEditor m_ScreenEditor;

    public Vector3 StringToVector3(string rString)
    {
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        var temp = rString.Substring(0, rString.Length).Split(',');
        if (temp.Count<string>() > 0)
            x = float.Parse(temp[0]);
        if (temp.Count<string>() > 1)
            y = float.Parse(temp[1]);
        if(temp.Count<string>() > 2)
            z = float.Parse(temp[2]);
        Vector3 rValue = new Vector3(x, y, z);

        return rValue;
    }

    // Member Methods
    void Start()
    {
        m_ScreenEditor = GetComponent<ScreenEditor>();

        float ppm = 0.0f;
        switch (m_ScreenEditor.m_Quality)
        {
            case ScreenEditor.Quality.Excelent:
                ppm = 1024;
                break;
            case ScreenEditor.Quality.Good:
                ppm = 512;
                break;
            case ScreenEditor.Quality.Average:
                ppm = 256;
                break;
            case ScreenEditor.Quality.Bad:
                ppm = 128;
                break;
            default:
                break;
        }

        int rtWidth = (int)(m_ScreenEditor.m_Width * ppm);
        int rtHeight = (int)(m_ScreenEditor.m_Height * ppm);

        // Create a new render texture
        m_RenderTex = new RenderTexture(rtWidth, rtHeight, 16);
        m_RenderTex.name = name + " RT";
        m_RenderTex.Create();

        // Get the render camera and set its target as the render texture
        m_RenderCamera = GetComponentInChildren<Camera>();
        m_RenderCamera.targetTexture = m_RenderTex;

        // Set it onto the material
        renderer.sharedMaterial.SetTexture("_MainTex", m_RenderTex);

        // Disable the Screen Editor
        m_ScreenEditor.enabled = false;

        SetupHardcodedUI();
    }

    void Update()
    {
        m_RenderTex.DiscardContents(true, true);
        RenderTexture.active = m_RenderTex;

        m_RenderCamera.Render();

        RenderTexture.active = null;
    }

    void SetupHardcodedUI()
    {
        ScreenEditor sE = GetComponent<ScreenEditor>();

        m_UI = new GameObject();
        m_UI.name = transform.name + "_UI";
        m_UI.transform.parent = m_RenderCamera.transform;
        m_UI.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
        m_UI.transform.localRotation = Quaternion.identity;
        m_UI.layer = LayerMask.NameToLayer("UI");



        TextAsset ta = Resources.Load("test", typeof(TextAsset)) as TextAsset;
        


        XmlDocument doc = new XmlDocument();
        XmlTextReader reader = new XmlTextReader(new StringReader(ta.text));
        doc.Load(reader);



        var Buttons =
            from XmlNode b in doc.LastChild.ChildNodes
            where b.Name == "button"
            select b;



        foreach (XmlNode button in Buttons)
        {
            // Create the button game object
            string asset = "Assets/InGame UI/Buttons/" + button.Attributes["type"].Value + ".prefab";
            GameObject buttonGo = (GameObject)Instantiate(Resources.LoadAssetAtPath(asset, typeof(GameObject)));

            // Set the default values
            buttonGo.transform.parent = m_UI.transform;
            buttonGo.transform.localPosition = Vector3.zero;
            buttonGo.transform.localRotation = Quaternion.identity;

            // Set the position
            if (button.Attributes["pos"] != null)
            {
                Vector3 pos = StringToVector3(button.Attributes["pos"].Value);
                buttonGo.transform.localPosition = new Vector3(pos.x * sE.m_Width, pos.y * sE.m_Height);
            }

            // Set the text
            if (button.Attributes["text"] != null)
                buttonGo.GetComponentInChildren<TextMesh>().text = button.Attributes["text"].Value;
        }







        //GameObject Button1 = (GameObject)Instantiate(Resources.LoadAssetAtPath("Assets/InGame UI/Buttons/SimpleButton.prefab", typeof(GameObject)));
        //Button1.transform.parent = m_UI.transform;
        //Button1.transform.localPosition = new Vector3(0.0f, sE.m_Height * 0.5f - Button1.GetComponent<ButtonEditor>().m_ButtonHeight * 0.45f);
        //Button1.transform.localRotation = Quaternion.identity;
        //Button1.GetComponentInChildren<TextMesh>().text = "Bounce";

        //GameObject Button2 = (GameObject)Instantiate(Resources.LoadAssetAtPath("Assets/InGame UI/Buttons/SimpleButton.prefab", typeof(GameObject)));
        //Button2.transform.parent = m_UI.transform;
        //Button2.transform.localPosition = new Vector3(0.0f, -sE.m_Height * 0.5f + Button1.GetComponent<ButtonEditor>().m_ButtonHeight * 0.45f);
        //Button2.transform.localRotation = Quaternion.identity;
        //Button2.GetComponentInChildren<TextMesh>().text = "Stop";

        //// Add some color buttons
        //GameObject cube = null;

        //GameObject Button3 = (GameObject)Instantiate(Resources.LoadAssetAtPath("Assets/InGame UI/Buttons/SimpleButton1.prefab", typeof(GameObject)));
        //Button3.transform.parent = m_UI.transform;
        //Button3.transform.localPosition = new Vector3(-sE.m_Width * 0.35f, -sE.m_Height * 0.3f);
        //Button3.transform.localRotation = Quaternion.identity;
        //cube = Button3.transform.GetChild(0).gameObject;
        //cube.renderer.material.color = Color.red;
        //cube.rigidbody.AddTorque(new Vector3(1.0f, -1.0f, 0.0f).normalized * 0.1f);

        //GameObject Button4 = (GameObject)Instantiate(Resources.LoadAssetAtPath("Assets/InGame UI/Buttons/SimpleButton1.prefab", typeof(GameObject)));
        //Button4.transform.parent = m_UI.transform;
        //Button4.transform.localPosition = new Vector3(sE.m_Width * 0.35f, -sE.m_Height * 0.3f);
        //Button4.transform.localRotation = Quaternion.identity;
        //cube = Button4.transform.GetChild(0).gameObject;
        //cube.renderer.material.color = Color.green;
        //cube.rigidbody.AddTorque(new Vector3(-1.0f, 0.0f, 1.0f).normalized * 0.1f);

        //GameObject Button5 = (GameObject)Instantiate(Resources.LoadAssetAtPath("Assets/InGame UI/Buttons/SimpleButton1.prefab", typeof(GameObject)));
        //Button5.transform.parent = m_UI.transform;
        //Button5.transform.localPosition = new Vector3(sE.m_Width * 0.35f, sE.m_Height * 0.3f);
        //Button5.transform.localRotation = Quaternion.identity;
        //cube = Button5.transform.GetChild(0).gameObject;
        //cube.renderer.material.color = Color.blue;
        //cube.rigidbody.AddTorque(new Vector3(1.0f, 1.0f, -1.0f).normalized * 0.1f);

        //GameObject Button6 = (GameObject)Instantiate(Resources.LoadAssetAtPath("Assets/InGame UI/Buttons/SimpleButton1.prefab", typeof(GameObject)));
        //Button6.transform.parent = m_UI.transform;
        //Button6.transform.localPosition = new Vector3(-sE.m_Width * 0.35f, sE.m_Height * 0.3f);
        //Button6.transform.localRotation = Quaternion.identity;
        //cube = Button6.transform.GetChild(0).gameObject;
        //cube.renderer.material.color = Color.yellow;
        //cube.rigidbody.AddTorque(new Vector3(-1.0f, -1.0f, 0.0f).normalized * 0.1f);
    }

    public void CheckButtonCollision(RaycastHit _rh)
    {
        ScreenEditor sE = GetComponent<ScreenEditor>();
        Vector3 offset = new Vector3(_rh.textureCoord.x * sE.m_Width - sE.m_Width * 0.5f,
                                     _rh.textureCoord.y * sE.m_Height - sE.m_Height * 0.5f,
                                     0.0f);

        offset = transform.rotation * offset;
        Vector3 rayOrigin = m_UI.transform.position + offset + transform.forward * -1.0f;

        Ray ray = new Ray(rayOrigin, transform.forward);
        RaycastHit hit;
        float rayLength = 2.0f;

        if (Physics.Raycast(ray, out hit, rayLength, 1 << LayerMask.NameToLayer("UI")))
        {
            ButtonUI bUI = hit.transform.parent.gameObject.GetComponent<ButtonUI>();
            if (bUI)
            {
                Debug.Log("Button Hit: " + hit.transform.parent.name);
                bUI.ButtonActivated();
            }
            else
            {
                Debug.Log("Button Hit: " + hit.transform.name);
            }

            Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.green, 0.5f);
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.red, 0.5f);
        }
    }
}
