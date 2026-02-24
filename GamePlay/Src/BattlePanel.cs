using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BattlePanel : BasePanel
{
    private int _timeLeft;
    private TMP_Text _timeText;
    private void Awake()
    {
        _timeLeft = 60;
        _timeText = Utils.GetNode(transform, "Text_CountDown").GetComponent<TMP_Text>();
        StartCoroutine(CountDown());
    }

    private IEnumerator CountDown()
    {
        while (true)
        {
            _timeLeft--;
            _timeText.text = _timeLeft.ToString();
            yield return new WaitForSeconds(1f);
        }
    }
}
