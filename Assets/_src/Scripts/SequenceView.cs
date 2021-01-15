using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace _src.Scripts
{
    public class SequenceView : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI attemptsText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image sequenceImage;
        [SerializeField] private List<Sprite> selectedIcons = new List<Sprite>();
        [SerializeField] private List<Sprite> icons = new List<Sprite>();
        [SerializeField] private Transform iconsGrid;

        [Header("Animation Settings")] 
        [SerializeField] private float animationTime;
        [SerializeField] private float fadeTime;
        [SerializeField] private float animationWaitingTime;
        [SerializeField] private float sequenceWaitingTime;

        private IDisposable _setup;
        private IDisposable _newGame;
        private IDisposable _show;
        private IDisposable _timer;
        private IDisposable _gameOver;

        private int _currentIcon;

        private int _attempts;
        private int attempts
        {
            get => _attempts;
            set
            {
                _attempts = value;
                attemptsText.text = $"Attempts : {attempts}";
            }
        }

        private int _time;

        private int time
        {
            get => _time;
            set
            {
                _time = value;
                timeText.text = $"Time : {_time}";
            }
        }


        public void Start()
        {
            MainThreadDispatcher.Initialize();
            SetupGame();
            newGameButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    StartNewGame();
                });
        }
        private void SetupGame()
        {
            _gameOver?.Dispose();
            _setup?.Dispose();
            _setup = Observable
                .FromCoroutine(SelectPlayableIcons)
                .SelectMany(SetupGridIcons)
                .SelectMany(AnimateGridIn)
                .Subscribe();
        }
        private IEnumerator SelectPlayableIcons()
        {
            selectedIcons.Clear();
            while (selectedIcons.Count < 6)
            {
                int random = Random.Range(0, icons.Count);
                if (!selectedIcons.Contains(icons[random]))
                    selectedIcons.Add(icons[random]);
            }

            yield return null;
        }
        private IEnumerator SetupGridIcons()
        {
            for (int i = 0; i < iconsGrid.childCount; i++)
            {
                iconsGrid.GetChild(i).GetComponent<Image>().sprite = selectedIcons[i];
            }
            
            yield return null;
        }
        private IEnumerator AnimateGridIn()
        {
            for (int i = 0; i < iconsGrid.childCount; i++)
            {
                Transform button = iconsGrid.GetChild(i);
                button.DOScale(Vector3.one, animationTime);
                button.GetComponent<Image>().DOColor(Color.white, fadeTime);
                yield return new WaitForSeconds(animationWaitingTime);
            }
        }
        private IEnumerator AnimateGridOut()
        {
            foreach (Sprite selectedIcon in selectedIcons)
            {
                for (int i = 0; i < iconsGrid.childCount; i++)
                {
                    if (selectedIcon != iconsGrid.GetChild(i).GetComponent<Image>().sprite) continue;
                    Transform button = iconsGrid.GetChild(i);
                    button.DOScale(Vector3.zero, animationTime);
                    button.GetComponent<Image>().DOColor(new Color(1,1,1,0), fadeTime);
                    yield return new WaitForSeconds(animationWaitingTime);
                }
            }
        }

        private void StartNewGame()
        {
            attempts = _currentIcon = 0;
            ResetTimer();
            _newGame?.Dispose();
            _newGame = Observable
                .FromCoroutine(CreateNewSequence)
                .SelectMany(Show)
                .Subscribe();

        }

        private void ResetTimer()
        {
            _timer?.Dispose();
            time = 0;
        }


        private IEnumerator CreateNewSequence()
        {
            Stack<Sprite> sequence = new Stack<Sprite>();
            while (sequence.Count < 6)
            {
                int random = Random.Range(0, selectedIcons.Count);
                if (!sequence.Contains(selectedIcons[random]))
                    sequence.Push(selectedIcons[random]);
            }
            
            selectedIcons.Clear();
            for (int i = 0; i < 6; i++)
            {
                selectedIcons.Add(sequence.Pop());
            }

            yield return null;
        }
        private IEnumerator Show()
        {
            _show?.Dispose();
            _show = Observable
                .FromCoroutine(MoveTitleTextUp)
                .SelectMany(ShowSequenceWindow)
                .SelectMany(ShowEachSequenceIcon)
                .SelectMany(HideSequenceWindow)
                .SelectMany(MoveTitleTextDown)
                .SelectMany(StartTimer)
                .Subscribe();

            yield return null;
        }
        private IEnumerator ShowSequenceWindow()
        {
            yield return new DOTweenCYInstruction
                .WaitForCompletion(sequenceImage.transform.parent
                    .DOScale(Vector2.one, animationTime));

        }
        private IEnumerator HideSequenceWindow()
        {
            yield return new DOTweenCYInstruction
                .WaitForCompletion(sequenceImage.transform.parent
                    .DOScale(Vector2.zero, animationTime));
        }
        private IEnumerator ShowEachSequenceIcon()
        {
            foreach (var icon in selectedIcons)
            {
                sequenceImage.DOColor(new Color(1, 1, 1, 0), fadeTime);
                sequenceImage.transform.DOScale(Vector2.zero, animationTime)
                    .OnComplete(() =>
                {
                    sequenceImage.sprite = icon;
                    sequenceImage.DOColor(Color.white, fadeTime);
                    sequenceImage.transform.DOScale(Vector2.one, animationTime);
                });
                yield return new WaitForSeconds(sequenceWaitingTime);
                sequenceImage.transform.DOScale(Vector2.zero, animationTime);
            }
        }
        private IEnumerator MoveTitleTextUp()
        {
            titleText.rectTransform.DOAnchorPosY(0f, animationTime);
            titleText.DOColor(new Color(1,1,1,0), fadeTime)
                .OnComplete(() =>
            {
                titleText.text = "Remember \n the icon sequence";
                titleText.DOColor(Color.white, fadeTime);
            });
            yield return null;
        }
        private IEnumerator MoveTitleTextDown()
        {
            titleText.rectTransform.DOAnchorPosY(-600, animationTime);
            titleText.DOColor(new Color(1,1,1,0), fadeTime)
                .OnComplete(() =>
            {
                titleText.text = "Tap icons \n in correct sequence";
                titleText.DOColor(Color.white, fadeTime);
            });
            yield return null;
        }
        
        private IEnumerator StartTimer()
        {
            _timer?.Dispose();
            _timer = Observable.Timer(TimeSpan.FromSeconds(1))
                .Repeat()
                .Subscribe(_ =>
                {
                    ++time;
                });
            yield return null;
        }

        public void CheckSequence(Button button)
        {
            if (_timer == null) return;
            if (selectedIcons[_currentIcon] == button.image.sprite)
            {
                button.interactable = false;
                ++_currentIcon;
                if (_currentIcon == selectedIcons.Count)
                {
                    EndGame();
                }
            }
            else
            {
                button.image.DOColor(Color.red, fadeTime)
                    .OnComplete(() =>
                {
                    button.image.DOColor(Color.white, fadeTime);
                });
            }

            ++attempts;
        }

        private void EndGame()
        {
            ResetTimer();
            titleText.DOColor(new Color(1, 1, 1, 0), fadeTime)
                .OnComplete(() =>
            {
                titleText.text = "You Win! \n Press New Game Button to play";
                titleText.DOColor(Color.white, fadeTime);
            });

            _gameOver?.Dispose();
            _gameOver = Observable
                .FromCoroutine(ActivateButtons)
                .SelectMany(AnimateGridOut)
                .Subscribe(_ =>
                {
                    SetupGame();
                });
        }

        private IEnumerator ActivateButtons()
        {
            for (int i = 0; i < iconsGrid.childCount; i++)
            {
                Transform button = iconsGrid.GetChild(i);
                button.GetComponent<Button>().interactable = true;
                button.DOScale(new Vector2(1.2f,1.2f), animationTime)
                    .OnComplete(() =>
                    {
                        button.DOScale(Vector3.one, animationTime);
                    });
            } 
            yield return new WaitForSeconds(sequenceWaitingTime);
        }
    }
}