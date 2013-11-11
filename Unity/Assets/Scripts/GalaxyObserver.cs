﻿using UnityEngine;
using System.Collections;

public class GalaxyObserver : MonoBehaviour
{
    void Start()
    {
        if (CNetwork.IsServer)   // If this is the server...
        {
            // Find the galaxy instance and register this object as an observer.
            CNetwork network = CNetwork.Instance; System.Diagnostics.Debug.Assert(network);
            CGame game = CGame.Instance; System.Diagnostics.Debug.Assert(game);
            CGalaxy galaxy = game.GetComponent<CGalaxy>(); System.Diagnostics.Debug.Assert(galaxy);

            // Depending on the type of model; it may use a mesh renderer, an animator, or something else.
            float observationRadius = 1.0f;
            {
                Rigidbody body = gameObject.GetComponent<Rigidbody>();
                if (body)
                {
                    Debug.Log("Got Rigidbody on " + gameObject.name);

                    observationRadius = Mathf.Sqrt(body.collider.bounds.extents.sqrMagnitude);
                }
                else
                {
                    Debug.Log("No Rigidbody on " + gameObject.name);

                    Collider collider = gameObject.GetComponent<Collider>();
                    if (collider)
                    {
                        Debug.Log("Got Collider on " + gameObject.name);

                        observationRadius = Mathf.Sqrt(collider.bounds.extents.sqrMagnitude);
                    }
                    else
                    {
                        Debug.Log("No Collider on " + gameObject.name);

                        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                        if (meshRenderer)
                        {
                            Debug.Log("Got MeshRenderer on " + gameObject.name);

                            observationRadius = Mathf.Sqrt(meshRenderer.bounds.extents.sqrMagnitude);
                        }
                        else
                        {
                            Debug.Log("No MeshRenderer on " + gameObject.name);

                            bool gotSomethingFromAnimator = false;
                            Animator anim = gameObject.GetComponent<Animator>();
                            if (anim)
                            {
                                if (anim.renderer)
                                {
                                    gotSomethingFromAnimator = true;
                                    Debug.Log("Got Animator.renderer on " + gameObject.name);
                                    observationRadius = Mathf.Sqrt(anim.renderer.bounds.extents.sqrMagnitude);
                                }
                                else if (anim.collider)
                                {
                                    gotSomethingFromAnimator = true;
                                    Debug.Log("Got Animator.collider on " + gameObject.name);
                                    observationRadius = Mathf.Sqrt(anim.collider.bounds.extents.sqrMagnitude);
                                }
                                else if (anim.rigidbody)
                                {
                                    gotSomethingFromAnimator = true;
                                    Debug.Log("Got Animator.rigidbody on " + gameObject.name);
                                    observationRadius = Mathf.Sqrt(anim.rigidbody.collider.bounds.extents.sqrMagnitude);
                                }
                                else
                                    Debug.Log("Nothing useful in Animator on " + gameObject.name);
                            }
                            else
                                Debug.Log("No Animator on " + gameObject.name);

                            if (!gotSomethingFromAnimator)
                            {
                                Debug.LogWarning("GalaxyObserver: Can not get anything useful from " + gameObject.name + ". Bounding sphere radius set to 1");
                            }
                        }
                    }
                }
            }

            galaxy.RegisterObserver(this.gameObject, observationRadius/*Mathf.Sqrt(this.gameObject.rigidbody.collider.bounds.extents.sqrMagnitude)*/);

            //textObject = new GameObject();
            //textObject.transform.parent = this.gameObject.transform;
            //textObject.transform.localPosition = Vector3.zero;
            //textObject.transform.localRotation = Quaternion.identity;
            //textObject.layer = gameObject.layer;

            //// Add the mesh renderer
            //MeshRenderer mr = textObject.AddComponent<MeshRenderer>();
            //mr.material = (Material)Resources.Load("Fonts/Couri", typeof(Material));

            //// Add the text mesh
            //tm = textObject.AddComponent<TextMesh>();
            //tm.fontSize = 72;
            //tm.characterSize = .125f;
            //tm.color = Color.white;
            //tm.font = (Font)Resources.Load("Fonts/Couri", typeof(Font));
            //tm.anchor = TextAnchor.MiddleCenter;
            //tm.offsetZ = 0.0f;
            //tm.text = "OHAI";
            //tm.fontStyle = FontStyle.Italic;
        }
    }

    //GameObject textObject;
    //TextMesh tm;
    //void Update()
    //{
    //    textObject.transform.position = gameObject.transform.position;
    //    if (Camera.current)
    //        textObject.transform.rotation = Quaternion.LookRotation(gameObject.transform.position - Camera.current.transform.position);

    //    CGalaxy galaxy = CGame.Instance.GetComponent<CGalaxy>();
    //    CGalaxy.SGridCellPos transformedCellPos = galaxy.PointToRelativeCell(gameObject.transform.position);
    //    CGalaxy.SGridCellPos untransformedCellPos = galaxy.PointToAbsoluteCell(gameObject.transform.position);
    //    tm.text = string.Format("Rel({0},{1},{2})\nAbs({3},{4},{5})", transformedCellPos.x, transformedCellPos.y, transformedCellPos.z, untransformedCellPos.x, untransformedCellPos.y, untransformedCellPos.z);
    //}

    void OnDestroy()
    {
        CNetwork network = CNetwork.Instance;
        if(network)
        {
            if (CNetwork.IsServer)
            {
                CGame game = CGame.Instance;
                if (game)
                {
                    CGalaxy galaxy = game.GetComponent<CGalaxy>();
                    if (galaxy)
                        galaxy.DeregisterObserver(this.gameObject);
                }
            }
        }
    }
}

public class GalaxyObserver_Attachable
{
    GameObject mObserver;
    GalaxyObserver_Attachable(GameObject observer, float observationRadius)
    {
        mObserver = observer;

        // Find parent galaxy instance and register this object as an observer.
        CGame.Instance.GetComponent<CGalaxy>().RegisterObserver(observer, observationRadius);
    }

    ~GalaxyObserver_Attachable()
    {
        CGame.Instance.GetComponent<CGalaxy>().DeregisterObserver(mObserver);
    }
}
