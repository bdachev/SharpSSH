// The MIT License (MIT)

// Copyright (c) 2020 Hans Wolff

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

public class AesCounterMode : SymmetricAlgorithm
{
    private readonly AesManaged _aes;

    public AesCounterMode()
    {
        _aes = new AesManaged
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None
        };
    }

    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] IV)
    {
        return new CounterModeCryptoTransform(_aes, rgbKey, IV);
    }

    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] IV)
    {
        return new CounterModeCryptoTransform(_aes, rgbKey, IV);
    }

    public override void GenerateKey()
    {
        _aes.GenerateKey();
    }

    public override void GenerateIV()
    {
        _aes.GenerateIV();
    }
}

sealed public class CounterModeCryptoTransform : ICryptoTransform
{
    private readonly byte[] _counter;
    private readonly ICryptoTransform _counterEncryptor;
    private readonly Queue<byte> _xorMask = new Queue<byte>();
    private readonly SymmetricAlgorithm _symmetricAlgorithm;

    public CounterModeCryptoTransform(SymmetricAlgorithm symmetricAlgorithm, byte[] key, byte[] iv)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _symmetricAlgorithm = symmetricAlgorithm ?? throw new ArgumentNullException(nameof(symmetricAlgorithm));
        _counter = new byte[_symmetricAlgorithm.BlockSize / 8];
        Debug.Assert(_counter.Length >= iv.Length);
        Array.Copy(iv, _counter, Math.Min(iv.Length, _counter.Length));

        var zeroIv = new byte[_symmetricAlgorithm.BlockSize / 8];
        _counterEncryptor = symmetricAlgorithm.CreateEncryptor(key, zeroIv);
    }

    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        var output = new byte[inputCount];
        TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
        return output;
    }

    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
        int outputOffset)
    {
        for (var i = 0; i < inputCount; i++)
        {
            if (NeedMoreXorMaskBytes())
            {
                EncryptCounterThenIncrement();
            }

            var mask = _xorMask.Dequeue();
            outputBuffer[outputOffset + i] = (byte) (inputBuffer[inputOffset + i] ^ mask);
        }

        return inputCount;
    }

    private bool NeedMoreXorMaskBytes()
    {
        return _xorMask.Count == 0;
    }

    private byte[] _counterModeBlock;
    private void EncryptCounterThenIncrement()
    {
        if (_counterModeBlock == null)
            _counterModeBlock = new byte[_symmetricAlgorithm.BlockSize / 8];

        _counterEncryptor.TransformBlock(_counter, 0, _counter.Length, _counterModeBlock, 0);
        IncrementCounter();

        foreach (var b in _counterModeBlock)
        {
            _xorMask.Enqueue(b);
        }
    }

    private void IncrementCounter()
    {
        for (int i = _counter.Length - 1; i >= 0; i--)
        {
            if (++_counter[i] != 0)
                break;
        }
    }

    public int InputBlockSize => _symmetricAlgorithm.BlockSize / 8;
    public int OutputBlockSize => _symmetricAlgorithm.BlockSize / 8;
    public bool CanTransformMultipleBlocks => true;
    public bool CanReuseTransform => false;

    public void Dispose()
    {
        _counterEncryptor.Dispose();
    }
}

//public void ExampleUsage()
//{
//    var key = new byte[16];
//    RandomNumberGenerator.Create().GetBytes(key);
//    var nonce = new byte[8];
//    RandomNumberGenerator.Create().GetBytes(nonce);
//
//    var dataToEncrypt = new byte[12345];
//
//    var counter = 0;
//    using var counterMode = new AesCounterMode(nonce, counter);
//    using var encryptor = counterMode.CreateEncryptor(key, null);
//    using var decryptor = counterMode.CreateDecryptor(key, null);
//
//    var encryptedData = new byte[dataToEncrypt.Length];
//    var bytesWritten = encryptor.TransformBlock(dataToEncrypt, 0, dataToEncrypt.Length, encryptedData, 0);
//
//    var decrypted = new byte[dataToEncrypt.Length];
//    decryptor.TransformBlock(encryptedData, 0, bytesWritten, decrypted, 0);
//
//    //decrypted.Should().BeEquivalentTo(dataToEncrypt);
//}
