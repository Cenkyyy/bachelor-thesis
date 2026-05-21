using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class StarterWordCategoryPanel : MonoBehaviour
{
    private struct ButtonOptionData
    {
        public WordData Word;
        public bool IsFixed;
    }

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private string _categoryLabel = "word";
    [SerializeField] private Transform _optionsRoot;
    [SerializeField] private StarterWordOptionButton _optionButtonPrefab;

    private readonly List<StarterWordOptionButton> _spawnedOptions = new();
    private readonly List<WordData> _selectedWords = new();
    private readonly Dictionary<StarterWordOptionButton, ButtonOptionData> _optionByButton = new();
    private int _requiredCount;

    public IReadOnlyList<WordData> SelectedWords => _selectedWords;
    public bool IsRequirementSatisfied => _selectedWords.Count >= _requiredCount;

    public event Action SelectionChanged;

    public void BuildButtonOptions<TWordData>(string categoryLabel, int requiredCount, IReadOnlyList<TWordData> fixedWords, IReadOnlyList<TWordData> choiceWords)
        where TWordData : WordData
    {
        _categoryLabel = categoryLabel;
        _requiredCount = Mathf.Max(requiredCount, fixedWords.Count);

        ClearOptions();
        _selectedWords.Clear();

        for (int i = 0; i < fixedWords.Count; i++)
        {
            var word = fixedWords[i];
            if (word == null)
                continue;

            _selectedWords.Add(word);
            SpawnButtonOption(word, isFixed: true, isSelected: true);
        }

        for (int i = 0; i < choiceWords.Count; i++)
        {
            var word = choiceWords[i];
            if (word != null)
                SpawnButtonOption(word, isFixed: false, isSelected: false);
        }

        UpdateTitle();
    }

    public List<TWordData> GetSelectedWords<TWordData>()
        where TWordData : WordData
    {
        var selected = new List<TWordData>(_selectedWords.Count);
        for (int i = 0; i < _selectedWords.Count; i++)
        {
            if (_selectedWords[i] is TWordData word)
                selected.Add(word);
        }

        return selected;
    }

    private void SpawnButtonOption(WordData word, bool isFixed, bool isSelected)
    {
        var button = Instantiate(_optionButtonPrefab, _optionsRoot);
        button.Bind(word, word.DisplayName, isFixed, isSelected);
        button.Clicked += HandleOptionClicked;

        _spawnedOptions.Add(button);
        _optionByButton[button] = new ButtonOptionData
        {
            Word = word,
            IsFixed = isFixed,
        };
    }

    private void HandleOptionClicked(StarterWordOptionButton button)
    {
        if (!_optionByButton.TryGetValue(button, out var option) || option.IsFixed)
            return;

        if (_selectedWords.Contains(option.Word))
        {
            _selectedWords.Remove(option.Word);
            button.SetSelected(false);
        }
        else if (_selectedWords.Count < _requiredCount)
        {
            _selectedWords.Add(option.Word);
            button.SetSelected(true);
        }

        UpdateTitle();
        SelectionChanged?.Invoke();
    }

    private void UpdateTitle()
    {
        if (_titleText != null)
            _titleText.text = $"Choose {_categoryLabel} ({_selectedWords.Count}/{_requiredCount})";
    }

    private void ClearOptions()
    {
        for (int i = 0; i < _spawnedOptions.Count; i++)
        {
            if (_spawnedOptions[i] == null)
                continue;

            _spawnedOptions[i].Clicked -= HandleOptionClicked;
            Destroy(_spawnedOptions[i].gameObject);
        }

        _spawnedOptions.Clear();
        _optionByButton.Clear();
    }
}
