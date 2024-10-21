﻿using System;
using InfoLabWPF.MVVM.Model;

public class RC5
{
    private int _wordSize;
    private int _rounds;
    private uint[] _S;
    private LinearCongruentialGenerator _lcg;

    public RC5(byte[] key, uint lcgModulus, uint lcgMultiplier, uint lcgIncrement, uint lcgSeed, int wordSize = 32, int rounds = 12)
    {

        _lcg = new LinearCongruentialGenerator(lcgModulus, lcgMultiplier, lcgIncrement, lcgSeed);
        Initialize(key, wordSize, rounds);
    }

    private void Initialize(byte[] key, int wordSize, int rounds)
    {
        _wordSize = wordSize;
        _rounds = rounds;

        int u = _wordSize / 8;
        int c = (key.Length + u - 1) / u;
        int t = 2 * (_rounds + 1);

        _S = new uint[t];
        _S[0] = 0xB7E15163;
        for (int i = 1; i < t; i++)
        {
            _S[i] = _S[i - 1] + 0x9E3779B9;
        }

        uint[] L = new uint[c];
        for (int i = 0; i < key.Length; i++)
        {
            L[i / u] = (L[i / u] << 8) + key[i];
        }

        int iA = 0, iB = 0;
        uint A = 0, B = 0;
        int iterations = 3 * Math.Max(c, t);
        for (int i = 0; i < iterations; i++)
        {
            A = _S[iA] = RotateLeft(_S[iA] + A + B, 3);
            B = L[iB] = RotateLeft(L[iB] + A + B, (int)(A + B));
            iA = (iA + 1) % t;
            iB = (iB + 1) % c;
        }
    }

    private uint RotateLeft(uint value, int count)
    {
        return (value << count) | (value >> (_wordSize - count));
    }

    private uint RotateRight(uint value, int count)
    {
        return (value >> count) | (value << (_wordSize - count));
    }

    private byte[] GenerateIVUsingLCG()
    {
        int blockSize = _wordSize / 8;
        byte[] iv = new byte[blockSize];
        for (int i = 0; i < iv.Length; i += 4)
        {
            uint nextValue = _lcg.Next();
            byte[] bytes = BitConverter.GetBytes(nextValue);
            Array.Copy(bytes, 0, iv, i, Math.Min(4, iv.Length - i));
        }
        return iv;
    }

    public void EncryptBlock(ref uint A, ref uint B)
    {
        A += _S[0];
        B += _S[1];
        for (int i = 1; i <= _rounds; i++)
        {
            A = RotateLeft(A ^ B, (int)B) + _S[2 * i];
            B = RotateLeft(B ^ A, (int)A) + _S[2 * i + 1];
        }
    }

    public void DecryptBlock(ref uint A, ref uint B)
    {
        for (int i = _rounds; i > 0; i--)
        {
            B = RotateRight(B - _S[2 * i + 1], (int)A) ^ A;
            A = RotateRight(A - _S[2 * i], (int)B) ^ B;
        }
        B -= _S[1];
        A -= _S[0];
    }

    public byte[] Encrypt(byte[] data)
    {
        int blockSize = _wordSize / 8;
        int paddedLength = ((data.Length + blockSize - 1) / blockSize) * blockSize;
        byte[] paddedData = new byte[paddedLength];
        Array.Copy(data, paddedData, data.Length);

        byte[] encrypted = new byte[paddedLength + blockSize];
        byte[] iv = GenerateIVUsingLCG();

        Console.WriteLine("Original IV: " + BitConverter.ToString(iv));

        // Encrypt IV using ECB mode and store it as the first block
        uint ivA = BitConverter.ToUInt32(iv, 0);
        uint ivB = BitConverter.ToUInt32(iv, 4);
        EncryptBlock(ref ivA, ref ivB);
        Array.Copy(BitConverter.GetBytes(ivA), 0, encrypted, 0, 4);
        Array.Copy(BitConverter.GetBytes(ivB), 0, encrypted, 4, 4);

        Console.WriteLine("Encoded IV: " + BitConverter.ToString(encrypted, 0, blockSize));

        byte[] prevBlock = new byte[blockSize];
        Array.Copy(iv, 0, prevBlock, 0, blockSize);

        for (int i = 0; i < paddedLength; i += blockSize)
        {
            uint A = BitConverter.ToUInt32(paddedData, i);
            uint B = BitConverter.ToUInt32(paddedData, i + 4);

            uint prevA = BitConverter.ToUInt32(prevBlock, 0);
            uint prevB = BitConverter.ToUInt32(prevBlock, 4);

            A ^= prevA;
            B ^= prevB;

            EncryptBlock(ref A, ref B);

            Array.Copy(BitConverter.GetBytes(A), 0, encrypted, i + blockSize, 4);
            Array.Copy(BitConverter.GetBytes(B), 0, encrypted, i + blockSize + 4, 4);

            Array.Copy(BitConverter.GetBytes(A), 0, prevBlock, 0, 4);
            Array.Copy(BitConverter.GetBytes(B), 0, prevBlock, 4, 4);
        }

        return encrypted;
    }

    public byte[] Decrypt(byte[] data)
    {
        int blockSize = _wordSize / 8;
        if (data.Length % blockSize != 0)
        {
            throw new ArgumentException("Invalid data length, must be a multiple of the block size.");
        }

        byte[] decrypted = new byte[data.Length - blockSize];

        // Decrypt the first block to retrieve the IV
        uint ivA = BitConverter.ToUInt32(data, 0);
        uint ivB = BitConverter.ToUInt32(data, 4);
        DecryptBlock(ref ivA, ref ivB);
        byte[] iv = new byte[blockSize];
        Array.Copy(BitConverter.GetBytes(ivA), 0, iv, 0, 4);
        Array.Copy(BitConverter.GetBytes(ivB), 0, iv, 4, 4);

        Console.WriteLine("Decoded IV: " + BitConverter.ToString(iv));

        byte[] prevBlock = new byte[blockSize];
        Array.Copy(iv, 0, prevBlock, 0, blockSize);

        for (int i = blockSize; i < data.Length; i += blockSize)
        {
            uint A = BitConverter.ToUInt32(data, i);
            uint B = BitConverter.ToUInt32(data, i + 4);

            uint originalA = A;
            uint originalB = B;

            DecryptBlock(ref A, ref B);

            uint prevA = BitConverter.ToUInt32(prevBlock, 0);
            uint prevB = BitConverter.ToUInt32(prevBlock, 4);

            A ^= prevA;
            B ^= prevB;

            Array.Copy(BitConverter.GetBytes(A), 0, decrypted, i - blockSize, 4);
            Array.Copy(BitConverter.GetBytes(B), 0, decrypted, i - blockSize + 4, 4);

            Array.Copy(BitConverter.GetBytes(originalA), 0, prevBlock, 0, 4);
            Array.Copy(BitConverter.GetBytes(originalB), 0, prevBlock, 4, 4);
        }

        return decrypted;
    }
}