using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiagnosisAPI : MonoBehaviour
{
    [SerializeField] WJ_Connector wj_conn;
    [SerializeField] CurrentStatus currentStatus;
    public CurrentStatus CurrentStatus => currentStatus; // ������Ƽ

    [Header("Panels")]
    [SerializeField] GameObject panel_diag_chooseDiff;  //���̵� ���� �г�
    [SerializeField] GameObject panel_question;         //���� �г�(����,�н�)

    [Header("Status")]
    int currentQuestionIndex;
    bool isSolvingQuestion;
    float questionSolveTime;

    [SerializeField] TEXDraw textEquation;           //���� �ؽ�Ʈ(TextDraw�� ���� ����)
    [SerializeField] Button[] btAnsr = new Button[4]; //���� ��ư��
    TEXDraw[] textAnsr;                  //���� ��ư�� �ؽ�Ʈ(TextDraw�� ���� ����)

    private const int SOLVE_TIME = 15; // ���� Ǯ�� �ð�
    private int _correctAnswerRemind; // ���� �ε��� ����
    private int _diagnosisIndex; // ���� �ε���
    private int _correctAnswers; // ���� ���� �� 
    private IEnumerator _timerCoroutine;

    private void Awake()
    {
        textAnsr = new TEXDraw[btAnsr.Length]; // ��ư ���ڸ�ŭ �Ҵ� TextDraw�� ����
        for (int i = 0; i < btAnsr.Length; ++i) // textAnsr�� text�Ҵ� 

            textAnsr[i] = btAnsr[i].GetComponentInChildren<TEXDraw>();

        _correctAnswerRemind = 0;
        _diagnosisIndex = 0;
        _correctAnswers = 0;
        _timerCoroutine = null;
    }

    void Start()
    {
        Setup();
    }

    void Update()
    {
        if (isSolvingQuestion) // ���� Ǯ�� ���϶� �ð� ���
        {
            questionSolveTime += Time.deltaTime;
        }
    }

    void Setup()
    {

        if (wj_conn != null)
        {
            if(!wj_conn._needDiagnosis)
               currentStatus = CurrentStatus.LEARNING;
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("Cannot find Connector");
#endif
        }
        switch (currentStatus)
        {
            case CurrentStatus.WAITING:
                panel_diag_chooseDiff.SetActive(true);
                if (wj_conn != null)
                {
                    wj_conn.onGetDiagnosis.AddListener(() => GetDiagnosis());
                    wj_conn.onGetLearning.AddListener(() => GetLearning(0));
                }
                break;
        }
    }

    /// <summary>
    /// ������ ���� �޾ƿ���
    /// </summary>
    void GetDiagnosis()
    {
        switch (wj_conn.cDiagnotics.data.prgsCd)
        {
            case "W":
                MakeQuestion(wj_conn.cDiagnotics.data.textCn,
                       wj_conn.cDiagnotics.data.qstCn,
                       wj_conn.cDiagnotics.data.qstCransr,
                       wj_conn.cDiagnotics.data.qstWransr);
                _diagnosisIndex++;
                break;
            case "E":
                Debug.Log("������ ��! �н� �ܰ�� �Ѿ�ϴ�.");
                FindObjectOfType<DrawLine>()?.ClearLIne();
                if (!PlayerPrefs.HasKey("Diagnosis")) // ���� �Ǵ��� ������
                {
                    PlayerPrefs.SetInt("Diagnosis", System.Convert.ToInt16(0)); // ���� �ʿ����� �������� ����
#if UNITY_EDITOR
                    Debug.LogWarning("���� �Ϸ�");
#endif
                }
                currentStatus = CurrentStatus.LEARNING;
                panel_question.SetActive(false); // ���ο� ������ �ޱ� ���ؼ� ��Ȱ��ȭ
                _correctAnswers = 0;
                break;
        }
    }

    /// <summary>
    ///  n ��° �н� ���� �޾ƿ���
    /// </summary>
    void GetLearning(int _index)
    {
        if (_index == 0)
        {
            currentQuestionIndex = 0;
        }

        MakeQuestion(wj_conn.cLearnSet.data.qsts[_index].textCn,
                wj_conn.cLearnSet.data.qsts[_index].qstCn,
                wj_conn.cLearnSet.data.qsts[_index].qstCransr,
                wj_conn.cLearnSet.data.qsts[_index].qstWransr);


    }

    /// <summary>
    /// �޾ƿ� �����͸� ������ ������ ǥ��
    /// </summary>
    void MakeQuestion(string textCn, string qstCn, string qstCransr, string qstWransr)
    {

        if (panel_diag_chooseDiff.activeSelf)
        {
            panel_diag_chooseDiff.SetActive(false);
        }

        panel_question.SetActive(true);

        // ù��° ���� �����̰ų� ù��° �н������϶���
        bool isFirstQuestion = (_diagnosisIndex == 0 && currentStatus == CurrentStatus.DIAGNOSIS) ||
            (currentQuestionIndex == 0 && currentStatus == CurrentStatus.LEARNING);

        if (isFirstQuestion)
        {
            SetupQuestion(textCn, qstCn, qstCransr, qstWransr);
            _diagnosisIndex++;
        }
        else
        {
            StartCoroutine(ColoringCorrectAnswer(textCn, qstCn, qstCransr, qstWransr, 0.5f));
            _diagnosisIndex++;
        }

    }

    void SetupQuestion(string textCn, string qstCn, string qstCransr, string qstWransr) // ���� ���� �Լ�
    {
        string correctAnswer;
        string[] wrongAnswers;

        textEquation.text = qstCn;

        correctAnswer = qstCransr;
        wrongAnswers = qstWransr.Split(',');

        int ansrCount = Mathf.Clamp(wrongAnswers.Length, 0, 3) + 1;

        for (int i = 0; i < btAnsr.Length; i++)
        {
            if (i < ansrCount)
                btAnsr[i].gameObject.SetActive(true);
            else
                btAnsr[i].gameObject.SetActive(false);
        }

        int ansrIndex = Random.Range(0, ansrCount);
        _correctAnswerRemind = ansrIndex; // ���� �ε��� �����صα�

        for (int i = 0, q = 0; i < ansrCount; ++i, ++q)
        {
            if (i == ansrIndex)
            {
                textAnsr[i].text = correctAnswer;
                --q;
            }
            else
                textAnsr[i].text = wrongAnswers[q];
        }
        isSolvingQuestion = true;
    }

    IEnumerator ColoringCorrectAnswer(string textCn, string qstCn, string qstCransr, string qstWransr, float delay) // ���� ���� �� ���� ǥ��
    {
        int prevIndex = _correctAnswerRemind; // ���� �ε��� ����
        textAnsr[_correctAnswerRemind].color = new Color(1.0f, 0.0f, 0.0f); // ���� �ε��� ���� ����

        yield return new WaitForSeconds(delay); // ������

        textAnsr[prevIndex].color = new Color(0.0f, 0.0f, 0.0f); // ���� ���� �ε��� �ٽ� ���� �ǵ�����
        SetupQuestion(textCn, qstCn, qstCransr, qstWransr);
    }

    /// <summary>
    /// ���� ������ �¾Ҵ� �� üũ
    /// </summary>
    public void SelectAnswer(int _idx = -1)
    {
        if (_idx == -1) // �ð��ʰ� ��
        {
            switch (currentStatus)
            {
                case CurrentStatus.DIAGNOSIS:

                    isSolvingQuestion = false;

                    wj_conn.Diagnosis_SelectAnswer("-1", "N", (int)(questionSolveTime * 1000));

                    questionSolveTime = 0;
                    break;

                case CurrentStatus.LEARNING:

                    isSolvingQuestion = false;
                    currentQuestionIndex++;

                    wj_conn.Learning_SelectAnswer(currentQuestionIndex, "-1", "N", (int)(questionSolveTime * 1000));

        

                    if (currentQuestionIndex >= 2) // ���� ����
                    {
                        panel_question.SetActive(false);
                    }
                    else
                    {
                        GetLearning(currentQuestionIndex);
                    }
                    questionSolveTime = 0;
                    break;
            }
            return;
        }

        bool isCorrect = false;
        string ansrCwYn = "N";
        // StopAllCoroutines();

        switch (currentStatus)
        {
            case CurrentStatus.DIAGNOSIS:
                isCorrect = textAnsr[_idx].text.CompareTo(wj_conn.cDiagnotics.data.qstCransr) == 0 ? true : false;
                ansrCwYn = isCorrect ? "Y" : "N";

                isSolvingQuestion = false;

                wj_conn.Diagnosis_SelectAnswer(textAnsr[_idx].text, ansrCwYn, (int)(questionSolveTime * 1000));

                questionSolveTime = 0;
                break;
        }

        if (isCorrect)
        {
            _correctAnswers += 1;
        }
    }



    // ���̵� ���� ��ư
    public void ButtonEvent_ChooseDifficulty(int a) // ������ �� ���̵� ���� ��ư(On Click���� �����������)
    {
        if (wj_conn._needDiagnosis)
        {
            currentStatus = CurrentStatus.DIAGNOSIS; // ���� ���� �������� ����
            wj_conn.FirstRun_Diagnosis(a); // ���̵� ���� 
        }
    }
}