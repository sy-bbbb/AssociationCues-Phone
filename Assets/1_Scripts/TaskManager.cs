using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine.UI;

public class TaskManager : MonoBehaviourPun
{
    [Header ("UI Pages")]
    public GameObject overviewPage;
    public GameObject fullLabelPage;

    [Header("UI Elements")]
    public TextMeshProUGUI fullLabelText;
    public Image colorTag;
    public Button closeButton;
    [SerializeField] private List<Button> labelThumbnails;

    private Transform content;
    private ContentSizeFitter contentSizeFitter;
    private PhotonView photonView;
    private List<string> allLabelTexts = new List<string>();

    private Player hmd;

    void Start()
    {
        content = fullLabelText.transform.parent;
        contentSizeFitter = content.GetComponent<ContentSizeFitter>();

        photonView = PhotonView.Get(this);

        labelThumbnails.Clear();
        foreach (Transform child in overviewPage.transform)
        {
            if (child.TryGetComponent<Button>(out Button button))
                labelThumbnails.Add(button);
        }

        ShowOverviewPage(true);
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        for (int i = 0; i < labelThumbnails.Count; i++)
        {
            int index = i;
            labelThumbnails[i].onClick.AddListener(() => OnThumbnailClicked(index));
        }
    }

    private void Update()
    {
        if (hmd == null)
            FindHmdPlayer();
    }

    private void FindHmdPlayer()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == "hmd")
            {
                hmd = player;
                Debug.Log("HMD player found!");
                return;
            }
        }
    }

    private void OnThumbnailClicked(int index)
    {
        photonView.RPC("RequestSelectObjectFromPhone", hmd, index);
        ShowOverviewPage(false);
    }

    private void OnCloseButtonClicked()
    {
        photonView.RPC("RequestDeselectFromPhone", hmd);
    }

    private void ShowOverviewPage(bool val)
    {
        overviewPage.SetActive(val);
        fullLabelPage.SetActive(!val);
    }

    public void SendRayHoldState(bool isHeld)
    {
        photonView.RPC("SetRayHold", RpcTarget.Others, isHeld);
    }

    public void SendRaySelectionRequest()
    {
        photonView.RPC("SelectWithRay", RpcTarget.Others);
    }

    [PunRPC]
    public void ReceiveInitialDataOnPhone(string[] labels)
    {
        Debug.Log("Received initial data. Populating thumbnails and storing texts.");

        allLabelTexts.Clear();
        allLabelTexts.AddRange(labels);

        for (int i = 0; i < labelThumbnails.Count; i++)
        {
            if (i < labels.Length)
            {
                labelThumbnails[i].GetComponentInChildren<TextMeshProUGUI>().text = labels[i];
                labelThumbnails[i].gameObject.SetActive(true);
            }
            else
            {
                labelThumbnails[i].gameObject.SetActive(false);
            }
        }
    }


    [PunRPC]
    public void ControlLabelOnPhone(bool show, int id, float[] colorArray, string customMessage)
    {
        ShowOverviewPage(!show);
        
        if (!string.IsNullOrEmpty(customMessage))
        {
            fullLabelText.text = customMessage;
            colorTag.gameObject.SetActive(false);
        }

        else if (show)
        {
            if (id >= 0 && id < allLabelTexts.Count)
                fullLabelText.text = allLabelTexts[id];
            else
                Debug.LogError($"Invalid label index: {id}");

            if (colorArray != null && colorArray.Length == 4)
            {
                colorTag.gameObject.SetActive(true);
                colorTag.color = new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]);
            }
            else
                colorTag.gameObject.SetActive(false);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentSizeFitter.transform);
            content.localPosition = new Vector3(content.localPosition.x, 0, content.localPosition.z);
        }
    }
}
