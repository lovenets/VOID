//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   AudioSystem.cs
//  Description :   Audio system implementation
//
//  Author  	:  Daniel Langsford
//  Mail    	:  folduppugg@hotmail.com
//

// Namespaces
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CAudioSystem : MonoBehaviour
{ 
	// Member Types
	public enum SoundType	
	{
		SOUND_EFFECTS,
		SOUND_MUSIC,
		SOUND_AMBIENCE,
		SOUND_VOICE,
	};
	
	public enum OcclusionState
	{
		OCCLUSION_FALSE,
		OCCLUSION_PARTIAL,
		OCCLUSION_FULL		
	};
	
	class ClipInfo
    {
	   	public float 		fadeInTime 		{ get; set; }
		public float 		fadeInTimer 	= 0;
		public float 		fadeOutTime 	{ get; set; }
		public float 		fadeOutTimer 	= 0;
		public AudioSource 	audioSource 	{ get; set; }
		public float 		defaultVolume 	= 1;
		public GameObject  	soundLocoation	{ get; set; }
		public SoundType	soundType;
		public bool 		useOcclusion	{ get; set; }	
    }

	// Member Delegates & Events
		
	// Member Properties
	public static CAudioSystem Instance
	{
		get { return (s_cInstance); }
	}	
	
	// Member Fields	
	static List<ClipInfo> s_activeAudio;
	
	static private float m_fMusicVolume = 0.75f;
	static private float m_fEffectsVolume = 0.75f;
	static private float m_fVoiceVolume = 1;
	static private float m_fAmbienceVolume = 0.75f;
	static private AudioListener m_listener;
	static private OcclusionState occludeState;
	
	static CAudioSystem s_cInstance = null;
	
	// Member Functions
	
	void Awake() 
	{
		s_cInstance = this;
		
        //Debug.Log("AudioManager Initialising");
       		
		s_activeAudio = new List<ClipInfo>();
		m_listener = (AudioListener) FindObjectOfType(typeof(AudioListener));
		
		occludeState = OcclusionState.OCCLUSION_FALSE;
    }
	
	void Update() 
	{
		if(m_listener == null)
		{
			m_listener = (AudioListener) FindObjectOfType(typeof(AudioListener));
		}
		else
		{
			ProcessActiveAudio();
		}

		//Debug grossness!
		if(Input.GetKeyDown(KeyCode.Keypad1))
		{
			BalanceVolumes(-1, m_fEffectsVolume - 0.1f, -1, -1);
		}
		if(Input.GetKeyDown(KeyCode.Keypad2))
		{
			BalanceVolumes(-1, -1, m_fAmbienceVolume - 0.1f, -1);
		}
		if(Input.GetKeyDown(KeyCode.Keypad3))
		{
			BalanceVolumes(-1, -1, -1, m_fVoiceVolume - 0.1f);
		}

	}
	
	void ProcessActiveAudio()
	{ 
	    var toRemove = new List<ClipInfo>();
	    //try 
		{
	        foreach(ClipInfo audioClip in s_activeAudio) 
			{
	            if(!audioClip.audioSource || audioClip.audioSource.isPlaying == false) 
				{
	                toRemove.Add(audioClip);
	            } 
				else
				{
					//process current volume scaling
					ScaleVolume(audioClip);

					//Process audio occlusion
					if(audioClip.soundType != SoundType.SOUND_AMBIENCE && audioClip.useOcclusion)			
					{
						ProcessAudioOcclusion(audioClip);					
					}

					//process fade in
					if(audioClip.fadeInTimer < audioClip.fadeInTime)
					{												
						audioClip.fadeInTimer += Time.deltaTime;
						float timeScale = audioClip.fadeInTimer / audioClip.fadeInTime;						
						audioClip.audioSource.volume =  audioClip.defaultVolume * timeScale;
						
						if(audioClip.audioSource.volume >= audioClip.defaultVolume)
						{

						}
					}
					
					//process fade out
					if(audioClip.fadeOutTimer < audioClip.fadeOutTime)
					{
																		
						audioClip.fadeOutTimer += Time.deltaTime;
						float timeScale = 1 - audioClip.fadeOutTimer / audioClip.fadeOutTime;						
						audioClip.audioSource.volume = audioClip.defaultVolume * timeScale;
						
						//Remove the sound once it has faded out
						if(audioClip.fadeOutTimer >= audioClip.fadeOutTime || audioClip.audioSource.volume == 0)
						{
							toRemove.Add(audioClip);
						}																	
					}					
				}
			}
	    } 
			
		    
		// Cleanup
	    foreach(var audioClip in toRemove) 
		{
	        s_activeAudio.Remove(audioClip);
			Destroy(audioClip.soundLocoation);
		}
    }

	void ScaleVolume(ClipInfo _currentClip)
	{
		switch(_currentClip.soundType)
		{
			case SoundType.SOUND_MUSIC:
			{
				//scale all music sounds by the new music volume 
				_currentClip.audioSource.volume = _currentClip.defaultVolume * m_fMusicVolume;
				break;
			}
				
			case SoundType.SOUND_EFFECTS:
			{
				//scale all sound effects by the SFX volume 
				_currentClip.audioSource.volume = _currentClip.defaultVolume * m_fEffectsVolume;
				break;
			}

			case SoundType.SOUND_VOICE:
			{
				//scale all sound effects by the SFX volume 
				_currentClip.audioSource.volume = _currentClip.defaultVolume * m_fVoiceVolume;
				break;
			}
				
			case SoundType.SOUND_AMBIENCE:
			{
				_currentClip.audioSource.volume = _currentClip.defaultVolume * m_fAmbienceVolume;	
				break;						
			}	
		}
	}
	
	void ProcessAudioOcclusion(ClipInfo _audioClip)
	{
		//Get the audioListener in the scene
		Vector3 listenerPos = m_listener.transform.position;
		Vector3 sourcePos = _audioClip.audioSource.transform.position;	
		
		int ignoreMask = 3 << 10;		
		ignoreMask = ~ignoreMask;
		
		RaycastHit hit;
        if(Physics.Linecast(sourcePos, listenerPos, out hit, ignoreMask))
		{
			Debug.DrawLine(	sourcePos, listenerPos, Color.cyan, 1.0f);
			
           	if(hit.collider.tag != "Listener")
			{	
				AudioLowPassFilter audioFilter = _audioClip.audioSource.gameObject.GetComponent<AudioLowPassFilter>(); 
								
				if(audioFilter == null)
				{
					AudioLowPassFilter filter =_audioClip.audioSource.gameObject.AddComponent<AudioLowPassFilter>();
					filter.cutoffFrequency = 2000; 
					_audioClip.audioSource.volume = _audioClip.defaultVolume * 0.5f;
					//Debug.Log (_audioClip.audioSource.clip.name + " occluded");
				}

			}			
			else
			{	
				if(_audioClip.audioSource.gameObject.GetComponent<AudioLowPassFilter>() != null)
				{
					Destroy(_audioClip.audioSource.gameObject.GetComponent<AudioLowPassFilter>());
					_audioClip.audioSource.volume = _audioClip.defaultVolume;
				}
				
				if(occludeState != OcclusionState.OCCLUSION_FALSE)
				{
					occludeState = OcclusionState.OCCLUSION_FALSE;
					//Debug.Log("No Occlusion");
				}	
			}

//				//TODO:
//				//For now, get every conduit in existence
//				GameObject[] conduits = GameObject.FindGameObjectsWithTag("AudioConduit");
//				bool occlude = true;
//				
//				//Before occluding, raycast from audio source to all nearby audio conduits.
//				foreach(GameObject conduit in conduits)
//				{
//					RaycastHit sourceToConduit;
//					if(Physics.Linecast(sourcePos, conduit.transform.position, out sourceToConduit))
//					{					
//						if(sourceToConduit.collider.tag == "AudioConduit")
//						{						
//							//If there is a conduit within sight of the audio source, check whether the listener has line of sight with the same conduit.				
//							RaycastHit LinstenerToConduit;
//							if(Physics.Linecast(listenerPos, conduit.transform.position, out LinstenerToConduit))
//							{							
//								if(LinstenerToConduit.collider.tag == "AudioConduit")
//								{
//									Debug.DrawLine(	sourcePos, conduit.transform.position, Color.red);
//									Debug.DrawLine(	conduit.transform.position, listenerPos, Color.blue);
//									
//									occlude = false;
//									_audioClip.audioSource.volume = _audioClip.defaultVolume / 2;
//									
//									if(occludeState != OcclusionState.OCCLUSION_PARTIAL)
//									{
//										occludeState = OcclusionState.OCCLUSION_PARTIAL;
//										Debug.Log("Partial Occlusion");
//									}									
//								}
//							}
//						}
//					}
//				}				
//				
//				AudioLowPassFilter audioFilter = _audioClip.audioSource.gameObject.GetComponent<AudioLowPassFilter>(); 
//				
//				if(occlude)
//				{
//					if(audioFilter == null)
//					{
//						AudioLowPassFilter filter =_audioClip.audioSource.gameObject.AddComponent<AudioLowPassFilter>();
//						filter.cutoffFrequency = 2000; 
//					}
//					
//					_audioClip.audioSource.volume = _audioClip.defaultVolume / 10;
//					
//					if(occludeState != OcclusionState.OCCLUSION_FULL)
//					{
//						occludeState = OcclusionState.OCCLUSION_FULL;
//						Debug.Log("Full Occlusion.  " + hit.collider.gameObject.name + " is blocking audio");
//					}					
//				}		
					 
		}
	}

//	public void SetOccludeAll(bool _bOccludeAll)
//	{
//		if(_bOccludeAll)
//		{
//			if(m_listener.gameObject.GetComponent<AudioLowPassFilter>() == null)
//			{
//				//m_listener.gameObject.AddComponent<AudioLowPassFilter>();
//			}
//		}
//		else
//		{
//			if(m_listener.gameObject.GetComponent<AudioLowPassFilter>() != null)
//			{
//				///Destroy(m_listener.gameObject.GetComponent<AudioLowPassFilter>());
//			}
//		}
//	}
	
	public static AudioSource Play(AudioClip _clip, Vector3 _soundOrigin, float _volume, float _pitch, bool _loop,
							float _fadeInTime, SoundType _soundType, bool _useOcclusion) 
	{
		//Create an empty game object
		GameObject soundLoc = new GameObject("Audio: " + _clip.name);
		soundLoc.transform.position = _soundOrigin;
		
		//Create the source
		AudioSource audioSource = soundLoc.AddComponent<AudioSource>();
		
		if(_fadeInTime > 0)
		{
			SetAudioSource(ref audioSource, _clip, 0);
		}
		else
		{
			SetAudioSource(ref audioSource, _clip, _volume);
		}
		audioSource.Play();
		
		// Set the audio to loop
		if(_loop) 
		{
			audioSource.loop = true;
		}
		else
		{
			Destroy(soundLoc, _clip.length);
		}
		
		//Set the source as active
		s_activeAudio.Add(new ClipInfo { fadeInTime = _fadeInTime, fadeInTimer = 0, audioSource = audioSource, defaultVolume = _volume,
										 soundLocoation = soundLoc, soundType = _soundType, useOcclusion = _useOcclusion});
		return(audioSource);
	}
	
	public static AudioSource Play(AudioClip _clip, Transform _emitter, float _volume, float _pitch, bool _loop,
							float _fadeInTime, SoundType _soundType, bool _useOcclusion) 
	{
		
		//Create the source
		AudioSource audioSource = Play(_clip, _emitter.position, _volume, _pitch, _loop, _fadeInTime, _soundType, _useOcclusion);
		audioSource.transform.parent = _emitter;
				
		return(audioSource);
	}
	
	public static AudioSource Play(AudioSource _source, float _volume, float _pitch, bool _loop, float _fadeInTime, SoundType _soundType,  bool _useOcclusion)
	{
		if(_fadeInTime > 0)
		{
			_source.volume = 0;
		}
		else
		{
			_source.volume = _volume;
		}
		
		_source.loop = _loop;
		
		s_activeAudio.Add(new ClipInfo { fadeInTime = _fadeInTime, fadeInTimer = 0, fadeOutTime = 0, audioSource = _source, defaultVolume = _volume,
										 soundType = _soundType, useOcclusion = _useOcclusion});
		
		_source.Play();
		
		return(_source);
	}

	public static AudioSource Play(AudioSource _source, float _volume, SoundType _soundType,  bool _useOcclusion)
	{

		_source.volume = _volume;
		
		_source.loop = false;
		
		s_activeAudio.Add(new ClipInfo { fadeInTime = 0, fadeInTimer = 0, fadeOutTime = 0, audioSource = _source, defaultVolume = _volume,
			soundType = _soundType, useOcclusion = _useOcclusion});
		
		_source.Play();
		
		return(_source);
	}
	
	private static void SetAudioSource(ref AudioSource _source, AudioClip _clip, float _volume) 
	{
		_source.rolloffMode = AudioRolloffMode.Linear;
		_source.dopplerLevel = 0.01f;
		_source.minDistance = 1.0f;
		_source.maxDistance = 5.0f;
		_source.clip = _clip;
		_source.volume = _volume;
	}
	
	public static void StopSound(AudioSource _toStop) 
	{
		try 
		{
			ClipInfo clip = s_activeAudio.Find(s => s.audioSource == _toStop);
            s_activeAudio.Remove(clip);
            _toStop.Stop();
		} 
		catch 
		{
			Debug.Log("Error trying to stop audio source " + _toStop);
		}
	}
	
	public static void FadeOut(AudioSource _toStop, float _fadeOutTime) 
	{
		if(_fadeOutTime == 0.0f)
		{
			_fadeOutTime = 0.1f;
		}
		
		s_activeAudio.Find(s => s.audioSource == _toStop).fadeOutTime = _fadeOutTime;	
	}

	//Pass in -1 if you don't wish to change a volume
	//For all other values, pass in a float between 0 and 1
	public static void BalanceVolumes(float _musicVolume, float _effectsVolume, float _ambienceVolume, float _voiceVolume)
	{
		if(_musicVolume > -1 && _musicVolume < 1)		  
		{
			m_fMusicVolume = _musicVolume;
		}
		if( _effectsVolume > -1 && _effectsVolume < 1)
		{
			m_fEffectsVolume = _effectsVolume;
		}
		if(_ambienceVolume > -1 && _ambienceVolume < 1)		  
		{
			m_fAmbienceVolume = _ambienceVolume;
		}
		if( _voiceVolume > -1 && _voiceVolume < 1)
		{
			m_fVoiceVolume = _voiceVolume;
		}
	}
	
	//Returns data from an AudioClip as a byte array.
	public static byte[] GetClipData(AudioClip _clip)
	{
		//Get data
		float[] floatData = new float[_clip.samples * _clip.channels];
		_clip.GetData(floatData,0);			
		
		//convert to byte array
		byte[] byteData = new byte[floatData.Length * 4];
		Buffer.BlockCopy(floatData, 0, byteData, 0, byteData.Length);
		
		return(byteData);
	}	
};
