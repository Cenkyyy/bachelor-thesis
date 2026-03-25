using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class StarterWordCategoryPanel : MonoBehaviour
{
    private struct ButtonOptionData
    {
        public int Value;
        public bool IsFixed;
    }

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private string _categoryLabel = "word";
    [SerializeField] private Transform _optionsRoot;
    [SerializeField] private StarterWordOptionButton _optionButtonPrefab;

    private readonly List<StarterWordOptionButton> _spawnedOptions = new();
    private readonly List<int> _selectedValues = new();
    private readonly Dictionary<StarterWordOptionButton, ButtonOptionData> _optionByButton = new();
    private int _requiredCount;

    public IReadOnlyList<int> SelectedValues => _selectedValues;
    public bool IsRequirementSatisfied => _selectedValues.Count >= _requiredCount;

    public event Action SelectionChanged;

    public void BuildButtonOptions<TWord>(string categoryLabel, int requiredCount, IReadOnlyList<TWord> fixedWords, IReadOnlyList<TWord> choiceWords, Func<TWord, string> labelProvider)
        where TWord : struct, Enum
    {
        _categoryLabel = categoryLabel;
        _requiredCount = Mathf.Max(requiredCount, fixedWords.Count);

        ClearOptions();
        _selectedValues.Clear();

        for (int i = 0; i < fixedWords.Count; i++)
        {
            var word = fixedWords[i];
            int value = Convert.ToInt32(word);
            _selectedValues.Add(value);
            SpawnButtonOption(value, labelProvider(word), isFixed: true, isSelected: true);
        }

        for (int i = 0; i < choiceWords.Count; i++)
        {
            var word = choiceWords[i];
            SpawnButtonOption(Convert.ToInt32(word), labelProvider(word), isFixed: false, isSelected: false);
        }

        UpdateTitle();
    }

    public List<TWord> GetSelectedWords<TWord>()
        where TWord : struct, Enum
    {
        var selected = new List<TWord>(_selectedValues.Count);
        for (int i = 0; i < _selectedValues.Count; i++)
            selected.Add((TWord)Enum.ToObject(typeof(TWord), _selectedValues[i]));

        selected.Sort();
        return selected;
    }

    private void SpawnButtonOption(int value, string label, bool isFixed, bool isSelected)
    {
        var button = Instantiate(_optionButtonPrefab, _optionsRoot);
        button.Bind(value, label, isFixed, isSelected);
        button.Clicked += HandleOptionClicked;

        _spawnedOptions.Add(button);
        _optionByButton[button] = new ButtonOptionData
        {
            Value = value,
            IsFixed = isFixed,
        };
    }

    private void HandleOptionClicked(StarterWordOptionButton button)
    {
        if (!_optionByButton.TryGetValue(button, out var option) || option.IsFixed)
            return;

        if (_selectedValues.Contains(option.Value))
        {
            _selectedValues.Remove(option.Value);
            button.SetSelected(false);
        }
        else if (_selectedValues.Count < _requiredCount)
        {
            _selectedValues.Add(option.Value);
            button.SetSelected(true);
        }

        UpdateTitle();
        SelectionChanged?.Invoke();
    }

    private void UpdateTitle()
    {
        if (_titleText != null)
            _titleText.text = $"Choose {_categoryLabel} ({_selectedValues.Count}/{_requiredCount})";
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
