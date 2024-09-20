﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using InfoLabWPF.MVVM.Model;


namespace InfoLabWPF.MVVM.ViewModel
{
    public class Lab1ViewModel : INotifyPropertyChanged
    {
        private LinearCongruentialGenerator _lcg;
        private GCDTest _gcdTest;
        private List<int> _sequence;
        private int _modulus;
        private int _multiplier;
        private int _increment;
        private int _seed;
        private int _sequenceCount;
        private double _piEstimate;
        private double _piBuiltInEstimate;
        private int _period;

        public Lab1ViewModel()
        {
            GenerateCommand = new RelayCommand(GenerateSequence);
            EstimatePiCommand = new RelayCommand(EstimatePi);
            EstimatePiBuiltInCommand = new RelayCommand(EstimatePiBuiltIn);
            LoadVariantDataCommand = new RelayCommand(LoadVariantData);
            FindPeriodCommand = new RelayCommand(FindPeriod);
            SaveSequenceCommand = new RelayCommand(SaveSequence);
        }

        public ICommand GenerateCommand { get; }
        public ICommand EstimatePiCommand { get; }
        public ICommand EstimatePiBuiltInCommand { get; }
        public ICommand LoadVariantDataCommand { get; }
        public ICommand FindPeriodCommand { get; }
        public ICommand SaveSequenceCommand { get; }

        public int Modulus
        {
            get => _modulus;
            set
            {
                if (value >= 0)
                {
                    _modulus = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Multiplier
        {
            get => _multiplier;
            set
            {
                if (value >= 0)
                {
                    _multiplier = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Increment
        {
            get => _increment;
            set
            {
                if (value >= 0)
                {
                    _increment = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Seed
        {
            get => _seed;
            set
            {
                if (value >= 0)
                {
                    _seed = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SequenceCount
        {
            get => _sequenceCount;
            set
            {
                if (value >= 0)
                {
                    _sequenceCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<int> Sequence
        {
            get => _sequence;
            set
            {
                _sequence = value;
                OnPropertyChanged();
            }
        }

        public double PiEstimate
        {
            get => _piEstimate;
            set
            {
                _piEstimate = value;
                OnPropertyChanged();
            }
        }

        public int Period
        {
            get => _period;
            set
            {
                _period = value;
                OnPropertyChanged();
            }
        }

        private void GenerateSequence()
        {
            if (Modulus <= 0 || Multiplier <= 0 || SequenceCount <= 0)
            {
                MessageBox.Show("Please ensure that Modulus, Multiplier, and Sequence Count are all positive values.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Multiplier >= Modulus || Increment >= Modulus || Seed >= Modulus)
            {
                MessageBox.Show("Multiplier, Increment, and Seed must be less than Modulus.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (Modulus >= int.MaxValue || Multiplier >= int.MaxValue || SequenceCount>= int.MaxValue)
            {
                MessageBox.Show($"Multiplier, Increment, and Seed must be less than {int.MaxValue}", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
                
            _lcg = new LinearCongruentialGenerator(_modulus, _multiplier, _increment, _seed);
            Sequence = new List<int>(_lcg.GenerateSequence(_sequenceCount));
            _gcdTest = new GCDTest(Sequence);
            OnPropertyChanged(nameof(Sequence));
        }

        private void EstimatePi()
        {
            if (_gcdTest != null)
            {
                PiEstimate = _gcdTest.EstimatePi();
                OnPropertyChanged(nameof(PiEstimate));
                OnPropertyChanged(nameof(PiDeviation)); 
            }
        }
        
        private void SaveSequence()
        {
            if (_lcg != null)
            {
                _lcg.SaveSequence(_sequence);
            }
        }
        
        public double PiBuiltInEstimate
        {
            get => _piBuiltInEstimate;
            set
            {
                _piBuiltInEstimate = value;
                OnPropertyChanged();
            }
        }

        private void EstimatePiBuiltIn()
        {
            var random = new Random();
            var generatedSequence = new List<int>();
            
            for (int i = 0; i < SequenceCount; i++)
            {
                generatedSequence.Add(random.Next(0, Modulus));
            }
            
            _gcdTest = new GCDTest(generatedSequence);
            PiBuiltInEstimate = _gcdTest.EstimatePi();
    
            OnPropertyChanged(nameof(PiBuiltInEstimate));
            OnPropertyChanged(nameof(PiBuiltInDeviation));
        }

        public double PiDeviation => PiEstimate == 0 ? 0 : Math.Abs(Math.PI - PiEstimate);
        public double PiBuiltInDeviation => PiBuiltInEstimate == 0 ? 0 : Math.Abs(Math.PI - PiBuiltInEstimate);

        private void FindPeriod()
        {
            if (_lcg != null)
            {
                Period = _lcg.FindPeriod();
                OnPropertyChanged(nameof(Period));
            }
        }
        
        private void LoadVariantData()
        {
            Modulus = (1 << 19) - 1;
            Multiplier = 6 * 6 * 6;
            Increment = 55;
            Seed = 1024;
            SequenceCount = 1000;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
