using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[System.Serializable]
public class QTEArea
{
    public string name;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale = Vector3.one;
    public Color gizmoColor = Color.red;
    public KeyCode keyCode = KeyCode.E;
}

public class QTESystem : MonoBehaviour
{
    [SerializeField] private List<QTEArea> qteAreas = new List<QTEArea>();
    [SerializeField] private TextMeshProUGUI keyDisplayText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private float slowMotionScale = 0.5f;
    
    public static event Action OnQTESuccess;
    public static event Action OnQTEFail;
    
    private Transform player;
    private PlayerMovement playerMovement;
    private HashSet<string> activeAreas = new HashSet<string>();
    private bool isSlowMotionActive = false;
    private bool isQTEActive = false;
    private KeyCode currentQTEKey = KeyCode.None;
    private string currentAreaName = "";
    private Coroutine progressCoroutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            player = FindObjectOfType<CharacterController>()?.transform;
        }
        
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        if (player == null) return;

        if (isQTEActive && Input.GetKeyDown(currentQTEKey))
        {
            QTESuccess();
            return;
        }

        bool playerInAnyArea = false;
        KeyCode currentKey = KeyCode.None;
        string areaName = "";

        foreach (var area in qteAreas)
        {
            bool isInArea = IsPlayerInArea(area);
            string areaKey = area.name;

            if (isInArea)
            {
                playerInAnyArea = true;
                currentKey = area.keyCode;
                areaName = area.name;
                
                if (!activeAreas.Contains(areaKey))
                {
                    activeAreas.Add(areaKey);
                    Debug.Log($"QTE Area: {area.name} | Position: {area.position} | Rotation: {area.rotation} | Scale: {area.scale} | Key: {area.keyCode} | Color: {area.gizmoColor}");
                }
            }
            else if (activeAreas.Contains(areaKey))
            {
                activeAreas.Remove(areaKey);
            }
        }

        if (playerInAnyArea && !isSlowMotionActive)
        {
            StartQTE(currentKey, areaName);
        }
        else if (!playerInAnyArea && isSlowMotionActive)
        {
            StopQTE();
        }
    }

    private void StartQTE(KeyCode key, string areaName)
    {
        isSlowMotionActive = true;
        isQTEActive = true;
        currentQTEKey = key;
        currentAreaName = areaName;
        
        Time.timeScale = slowMotionScale;
        
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        ShowKeyPrompt(key);
        ShowProgressSlider();
        StartCoroutine(QTETimer());
        progressCoroutine = StartCoroutine(UpdateProgressSlider());
    }

    private void StopQTE()
    {
        isSlowMotionActive = false;
        isQTEActive = false;
        currentQTEKey = KeyCode.None;
        currentAreaName = "";
        
        Time.timeScale = 1f;
        
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        HideKeyPrompt();
        HideProgressSlider();
        
        if (progressCoroutine != null)
        {
            StopCoroutine(progressCoroutine);
            progressCoroutine = null;
        }
    }

    private void QTESuccess()
    {
        Debug.Log("Başardın!");
        OnQTESuccess?.Invoke();
        RemoveCurrentArea();
        StopQTE();
    }

    private void QTEFail()
    {
        Debug.Log("Başaramadın!");
        OnQTEFail?.Invoke();
        RemoveCurrentArea();
        StopQTE();
    }

    private void RemoveCurrentArea()
    {
        for (int i = qteAreas.Count - 1; i >= 0; i--)
        {
            if (qteAreas[i].name == currentAreaName)
            {
                qteAreas.RemoveAt(i);
                break;
            }
        }
        activeAreas.Remove(currentAreaName);
    }

    private IEnumerator QTETimer()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (isQTEActive)
        {
            QTEFail();
        }
    }

    private void ShowKeyPrompt(KeyCode key)
    {
        if (keyDisplayText != null)
        {
            keyDisplayText.text = key.ToString();
            keyDisplayText.gameObject.SetActive(true);
        }
    }

    private void HideKeyPrompt()
    {
        if (keyDisplayText != null)
        {
            keyDisplayText.gameObject.SetActive(false);
        }
    }

    private void ShowProgressSlider()
    {
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
            progressSlider.gameObject.SetActive(true);
        }
    }

    private void HideProgressSlider()
    {
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(false);
        }
    }

    private IEnumerator UpdateProgressSlider()
    {
        float elapsed = 0f;
        float duration = 2f;
        
        while (elapsed < duration && isQTEActive)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }
            
            yield return null;
        }
        
        if (progressSlider != null)
        {
            progressSlider.value = 1f;
        }
    }

    private bool IsPlayerInArea(QTEArea area)
    {
        Vector3 localPos = Quaternion.Euler(-area.rotation) * (player.position - area.position);
        
        return Mathf.Abs(localPos.x) <= area.scale.x * 0.5f &&
               Mathf.Abs(localPos.y) <= area.scale.y * 0.5f &&
               Mathf.Abs(localPos.z) <= area.scale.z * 0.5f;
    }

    void OnDrawGizmos()
    {
        foreach (var area in qteAreas)
        {
            Gizmos.color = area.gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(area.position, Quaternion.Euler(area.rotation), area.scale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}