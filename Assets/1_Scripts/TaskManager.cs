using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviourPunCallbacks
{
    [Header ("UI Pages")]
    [SerializeField] private GameObject overviewPage;
    [SerializeField] private GameObject fullLabelPage;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI fullLabelText;
    [SerializeField]private Image colorTag;
    [SerializeField] private Button backButton;
    [SerializeField] private List<Button> labelThumbnails;

    private PhotonView pv;
    private List<string> allLabelTexts = new List<string>();
    private Player hmd;


    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
        PopulateAndSetupThumbnails();
        ShowInitialPage();
        FindHmdPlayer();
    }

    #region PUN Callbacks

    public override void OnJoinedRoom()
    {
        CheckForExistingPhoneConnection();
    }

    private void CheckForExistingPhoneConnection()
    {
        Player player = PhotonNetwork.PlayerListOthers.FirstOrDefault(p => p.NickName == NetworkManager.HMD_NICKNAME);
        hmd = player;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (newPlayer.NickName == NetworkManager.HMD_NICKNAME && hmd == null)
        {
            hmd = newPlayer;
            Debug.Log("HMD player reference set.");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer == hmd)
            HandleHmdDisconnection();
    }

    private void FindHmdPlayer()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == NetworkManager.HMD_NICKNAME)
            {
                hmd = player;
                Debug.Log("HMD player found in the room.");
                return;
            }
        }
        Debug.LogWarning("HMD player not found in the current room.");
    }
    private void HandleHmdDisconnection()
    {
        hmd = null;
        ShowInitialPage();
        Debug.LogWarning("HMD disconnected");
    }
    #endregion

    #region UI Control Methods
    public void ShowInitialPage()
    {
        overviewPage.SetActive(false);
        fullLabelPage.SetActive(false);
    }

    private void ShowOverviewPage()
    {
        overviewPage.SetActive(true);
        fullLabelPage.SetActive(false);
    }

    private void ShowFullLabelPage()
    {
        overviewPage.SetActive(false);
        fullLabelPage.SetActive(true);
    }
    #endregion

    #region RPC Senders
    public void SendRayHoldState(bool isHeld)
    {
        if (hmd != null)
            pv.RPC("SetRayHold", RpcTarget.Others, isHeld);
    }

    public void SendRaySelectionRequest()
    {
        if (hmd != null)
            pv.RPC("SelectWithRay", RpcTarget.Others);
    }
    #endregion

    #region PunRPC Receivers
    [PunRPC]
    public void ReceiveInitialDataOnPhone(string[] labels)
    {
        Debug.Log("Received initial data. Populating thumbnails and storing texts.");

        allLabelTexts.Clear();
        allLabelTexts.AddRange(labels);

        for (int i = 0; i < labelThumbnails.Count; i++)
        {
            bool hasData = i < labels.Length;
            labelThumbnails[i].gameObject.SetActive(hasData);

            if (hasData)
            {
                TextMeshProUGUI labelText = labelThumbnails[i].GetComponentInChildren<TextMeshProUGUI>();
                if (labelText != null)
                    labelText.text = labels[i];
            }
        }
    }

    [PunRPC]
    public void InitialiseUIOnPhone()
    {
        ShowOverviewPage();
    }

    [PunRPC]
    public void ControlLabelOnPhone(bool show, int id, float[] colorArray, string customMessage)
    {
        if (show)
        {
            if (!string.IsNullOrEmpty(customMessage))
            {
                fullLabelText.text = customMessage;
                colorTag.gameObject.SetActive(false);
            }
            else
            {
                if (id >= 0 && id < allLabelTexts.Count)
                {
                    fullLabelText.text = allLabelTexts[id];
                }
                else
                {
                    Debug.LogError($"Invalid label index received: {id}");
                    fullLabelText.text = "Error: Invalid Data Received";
                }

                bool hasColor = colorArray != null && colorArray.Length == 4;
                colorTag.gameObject.SetActive(hasColor);
                if (hasColor)
                {
                    colorTag.color = new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]);
                }
            }

            ShowFullLabelPage();
            StartCoroutine(RebuildLayout());
        }
        else
        {
            ShowOverviewPage();
        }
    }
    #endregion

    #region Private Helper Methods
    private void PopulateAndSetupThumbnails()
    {
        if (labelThumbnails == null || labelThumbnails.Count == 0)
        {
            labelThumbnails = new List<Button>();
            foreach (Transform child in overviewPage.transform)
            {
                if (child.TryGetComponent<Button>(out Button button))
                    labelThumbnails.Add(button);
            }
        }

        for (int i = 0; i < labelThumbnails.Count; i++)
        {
            int index = i;
            labelThumbnails[i].onClick.AddListener(() => OnThumbnailClicked(index));
        }
    }

    private void OnThumbnailClicked(int index)
    {
        if (hmd != null)
            pv.RPC("RequestSelectObjectFromPhone", hmd, index);
        else
            Debug.LogWarning("Cannot send RPC: HMD player not found.");
        ShowFullLabelPage();
    }

    private void OnBackButtonClicked()
    {
        if (hmd != null)
            pv.RPC("RequestDeselectFromPhone", hmd);
        else
            Debug.LogWarning("Cannot send RPC: HMD player not found.");
    }

    private IEnumerator RebuildLayout()
    {
        yield return new WaitForEndOfFrame();

        var contentRect = fullLabelText.transform.parent as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            contentRect.localPosition = new Vector3(contentRect.localPosition.x, 0, contentRect.localPosition.z);
        }
    }
    #endregion
}
