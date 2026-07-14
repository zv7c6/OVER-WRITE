using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class DialogueManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Button clickAreaButton;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;

    [Header("배경 UI Images")]
    public Image[] backgroundImages;

    [Header("캐릭터 UI Images")]
    public Image[] characterImages;

    [Header("버튼")]
    public Button backButton;
    public Button skipButton;
    public GameObject trollPanel;

    [Header("선택지 UI")]
    public GameObject choicePanel;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceTexts;

    [Header("페이드")]
    public Image fadeImage;
    public float fadeDuration = 0.7f;

    [Header("설정")]
    public float typingSpeed = 0.05f;

    [Header("엔딩")]
    public GameObject endingPanel;
    public Button quitButton;
    public Button titleButton;
    public string titleSceneName = "Title";

    [System.Serializable]
    public class DialogueData
    {
        [TextArea(2, 4)]
        public string dialogue;
        public string characterName;
        public Image background;
        public Image[] showCharacters;
        public bool fadeBefore;
        public bool returnToChoice;
        public bool instantText;
        public bool autoNext;
        public float autoNextDelay = 0.5f;
        public int returnIdx;
        public bool isChoice;
        public string[] choiceTexts;
        public int[] choiceNextIdx;
        public bool[] choiceFade;
        public Vector2 choicePanelPosition;
        public bool isEnding;
    }

    public DialogueData[] dialogues;

    private int currentIdx = 0;
    private Stack<int> history = new Stack<int>();
    private Coroutine typingCoroutine;
    private bool isTypingDone = false;
    private HashSet<int> usedChoices = new HashSet<int>();
    private Image currentBackground;

    void Start()
    {
        choicePanel.SetActive(false);
        fadeImage.color = new Color(0, 0, 0, 0);
        trollPanel.SetActive(false);
        endingPanel.SetActive(false);

        foreach (Image img in backgroundImages)
            img.gameObject.SetActive(false);

        foreach (Image img in characterImages)
            img.gameObject.SetActive(false);

        clickAreaButton.onClick.AddListener(OnScreenClicked);
        backButton.onClick.AddListener(OnBackClicked);
        skipButton.onClick.AddListener(() => trollPanel.SetActive(true));
        trollPanel.GetComponent<Button>().onClick.AddListener(() => trollPanel.SetActive(false));
        quitButton.onClick.AddListener(() => Application.Quit());
        titleButton.onClick.AddListener(() => SceneManager.LoadScene(titleSceneName));

        ShowCurrentDialogue();
    }

    void OnBackClicked()
    {
        if (choicePanel.activeSelf) return;
        if (history.Count == 0) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        int prevId = history.Peek();
        if (dialogues[prevId].isChoice)
        {
            for (int i = 0; i < dialogues[prevId].choiceTexts.Length; i++)
            {
                int choiceId = i + prevId * 100;
                usedChoices.Remove(choiceId);
            }
        }

        currentIdx = history.Pop();
        ShowCurrentDialogue();
    }

    void OnScreenClicked()
    {
        if (choicePanel.activeSelf) return;
        if (trollPanel.activeSelf) return;
        if (dialogues[currentIdx].isEnding) return;
        if (choicePanel.activeSelf) return;
        if (trollPanel.activeSelf) return;

        if (!isTypingDone)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = dialogues[currentIdx].dialogue;
            isTypingDone = true;

            if (dialogues[currentIdx].isChoice &&
                dialogues[currentIdx].choiceTexts != null &&
                dialogues[currentIdx].choiceTexts.Length > 0)
                ShowChoices();
            return;
        }

        if (dialogues[currentIdx].isChoice &&
            dialogues[currentIdx].choiceTexts != null &&
            dialogues[currentIdx].choiceTexts.Length > 0)
        {
            ShowChoices();
            return;
        }

        if (dialogues[currentIdx].returnToChoice)
        {
            history.Push(currentIdx);
            currentIdx = dialogues[currentIdx].returnIdx;
            ApplyDialogue(dialogues[currentIdx]);
            return;
        }

        if (dialogues[currentIdx].choiceNextIdx != null &&
            dialogues[currentIdx].choiceNextIdx.Length > 0)
        {
            history.Push(currentIdx);
            currentIdx = dialogues[currentIdx].choiceNextIdx[0];
            ShowCurrentDialogue();
            return;
        }

        history.Push(currentIdx);
        currentIdx++;
        if (currentIdx >= dialogues.Length) return;
        ShowCurrentDialogue();
    }

    void ShowCurrentDialogue()
    {
        DialogueData data = dialogues[currentIdx];

        if (data.fadeBefore)
            StartCoroutine(FadeAndShow(data));
        else
            ApplyDialogue(data);
    }

    IEnumerator FadeAndShow(DialogueData data)
    {
        clickAreaButton.interactable = false;
        yield return StartCoroutine(Fade(0, 1));
        ApplyDialogue(data);
        yield return StartCoroutine(Fade(1, 0));
        clickAreaButton.interactable = true;
    }

    void ApplyDialogue(DialogueData data)
    {
        if (data.background != null)
        {
            if (currentBackground != null)
                currentBackground.gameObject.SetActive(false);

            currentBackground = data.background;
            currentBackground.gameObject.SetActive(true);
        }

        foreach (Image img in characterImages)
            img.gameObject.SetActive(false);

        if (data.showCharacters != null)
        {
            foreach (Image img in data.showCharacters)
            {
                if (img != null)
                    img.gameObject.SetActive(true);
            }
        }

        if (!string.IsNullOrEmpty(data.characterName))
        {
            nameText.gameObject.SetActive(true);
            nameText.text = data.characterName;
        }
        else
        {
            nameText.gameObject.SetActive(false);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTypingDone = false;
        typingCoroutine = StartCoroutine(TypeText(data.dialogue));
    }

    void ShowChoices()
    {
        DialogueData data = dialogues[currentIdx];
        choicePanel.SetActive(true);
        choicePanel.GetComponent<RectTransform>().anchoredPosition = data.choicePanelPosition;

        int visibleIdx = 0;
        for (int i = 0; i < data.choiceTexts.Length; i++)
        {
            int choiceId = i + currentIdx * 100;

            if (usedChoices.Contains(choiceId)) continue;

            if (visibleIdx < choiceButtons.Length)
            {
                var oldTrigger = choiceButtons[visibleIdx].GetComponent<EventTrigger>();
                if (oldTrigger != null) Destroy(oldTrigger);

                choiceButtons[visibleIdx].gameObject.SetActive(true);
                choiceTexts[visibleIdx].text = data.choiceTexts[i];
                choiceTexts[visibleIdx].color = Color.gray;

                EventTrigger trigger = choiceButtons[visibleIdx].gameObject.AddComponent<EventTrigger>();
                int capturedVisible = visibleIdx;

                var enterEntry = new EventTrigger.Entry();
                enterEntry.eventID = EventTriggerType.PointerEnter;
                enterEntry.callback.AddListener((data2) => {
                    choiceTexts[capturedVisible].color = Color.white;
                });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry();
                exitEntry.eventID = EventTriggerType.PointerExit;
                exitEntry.callback.AddListener((data2) => {
                    choiceTexts[capturedVisible].color = Color.gray;
                });
                trigger.triggers.Add(exitEntry);

                int nextIdx = data.choiceNextIdx[i];
                bool useFade = data.choiceFade != null &&
                               i < data.choiceFade.Length &&
                               data.choiceFade[i];

                choiceButtons[visibleIdx].onClick.RemoveAllListeners();
                int capturedId = choiceId;
                choiceButtons[visibleIdx].onClick.AddListener(() => {
                    usedChoices.Add(capturedId);
                    OnChoiceSelected(nextIdx, useFade);
                });

                visibleIdx++;
            }
        }

        for (int i = visibleIdx; i < choiceButtons.Length; i++)
            choiceButtons[i].gameObject.SetActive(false);
    }

    void OnChoiceSelected(int nextIdx, bool useFade)
    {
        choicePanel.SetActive(false);
        history.Push(currentIdx);

        if (useFade)
            StartCoroutine(FadeTransition(nextIdx));
        else
        {
            clickAreaButton.interactable = true;
            currentIdx = nextIdx;
            ShowCurrentDialogue();
        }
    }

    IEnumerator FadeTransition(int nextIdx)
    {
        clickAreaButton.interactable = false;
        yield return StartCoroutine(Fade(0, 1));
        currentIdx = nextIdx;
        ApplyDialogue(dialogues[currentIdx]);
        yield return StartCoroutine(Fade(1, 0));
        clickAreaButton.interactable = true;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, to);
    }

    IEnumerator TypeText(string fullText)
    {
        dialogueText.text = "";

        if (dialogues[currentIdx].instantText)
        {
            dialogueText.text = fullText;
        }
        else
        {
            foreach (char letter in fullText)
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTypingDone = true;

        if (dialogues[currentIdx].isEnding)
        {
            StartCoroutine(PlayEnding());
            yield break;
        }

        if (dialogues[currentIdx].isChoice &&
            dialogues[currentIdx].choiceTexts != null &&
            dialogues[currentIdx].choiceTexts.Length > 0)
        {
            ShowChoices();
            yield break;
        }

        if (dialogues[currentIdx].autoNext)
        {
            yield return new WaitForSeconds(dialogues[currentIdx].autoNextDelay);
            OnScreenClicked();
        }
    }

    IEnumerator PlayEnding()
    {
        clickAreaButton.interactable = false;
        yield return StartCoroutine(Fade(0, 1));
        // 크레딧 내용 원하시면 여기 수정
        dialogueText.text = "지금까지 플레이 해주셔서 감사합니다. 타이틀로 돌아가 다른 캐릭터를 체험하실수 있습니다.";
        nameText.gameObject.SetActive(false);
        yield return StartCoroutine(Fade(1, 0));
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(Fade(0, 1));
        endingPanel.SetActive(true);
        yield return StartCoroutine(Fade(1, 0));
    }


}
