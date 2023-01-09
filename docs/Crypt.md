# Crypt

## KeyDerivation
### Prepend
```
?prepend@?1??GetParams@Secret@CryptoPolicy@shared@@SA?AUSecretExchangeParams@4@XZ@4QBEB
0x0D6, 0x37, 0x0F1, 0x0AA, 0x0E2, 0x0F0, 0x41, 0x8C
```

### Append
```
?append@?1??GetParams@Secret@CryptoPolicy@shared@@SA?AUSecretExchangeParams@4@XZ@4QBEB
0x0A8, 0x0F8, 0x1A, 0x57, 0x4E, 0x22, 0x8A, 0x0B7
```

### shared::SecretExchangeParams
```cpp
struct shared::SecretExchangeParams
{
  DWORD curveType;
  QWORD *hmacSalt;
  QWORD *hmacSaltEnd;
  DWORD *reserved3;
  QWORD *prepend;
  QWORD *prependEnd;
  DWORD reserved6;
  QWORD *append;
  QWORD *appendEnd;
  QWORD *reserved9;
};
```

### shared::CryptoPolicy::Secret::GetParams
```cpp
shared::SecretExchangeParams *__fastcall shared::CryptoPolicy::Secret::GetParams(shared::SecretExchangeParams *params)
{
  QWORD **p_prepend; // rcx

  params->hmacSalt = 0i64;
  params->reserved3 = 0i64;
  p_prepend = &params->prepend;
  *p_prepend = 0i64;
  p_prepend[1] = 0i64;
  p_prepend[2] = 0i64;
  params->append = 0i64;
  params->appendEnd = 0i64;
  params->reserved9 = 0i64;
  params->curveType = 2;
  params->hmacSaltEnd = params->hmacSalt;
  std::vector<unsigned char,std::allocator<unsigned char>>::_Assign_range<unsigned char const *>(
    p_prepend,
    &`shared::CryptoPolicy::Secret::GetParams'::`2'::prepend,
    &`shared::CryptoPolicy::Secret::GetParams'::`2'::append);
  std::vector<unsigned char,std::allocator<unsigned char>>::_Assign_range<unsigned char const *>(
    &params->append,
    &`shared::CryptoPolicy::Secret::GetParams'::`2'::append,
    "onecoreuap\\windows\\cdp\\shared\\CryptoPolicy.h");
  return params;
}
```

### shared::AsymmetricKey::SecretExchange
```cpp
PUCHAR *__fastcall shared::AsymmetricKey::SecretExchange(
        shared::AsymmetricKey *this,
        PUCHAR *result,
        BCRYPT_KEY_HANDLE *remoteKey,
        shared::SecretExchangeParams *secretParams)
{
  __int64 v8; // rdx
  __int64 v9; // rax
  __int64 v10; // rax
  __int64 v11; // rax
  __int64 v12; // rdx
  __int64 v13; // rax
  __int64 v14; // rax
  DWORD curveType; // esi
  wchar_t *hashAlgorithm; // r14
  size_t v17; // rdi
  __int64 strLen; // rax
  ULONG bufferCount; // ecx
  QWORD *hmacSalt; // rdx
  QWORD *hmacSaltEnd; // rax
  QWORD *secretPrepend; // r8
  QWORD *prependEnd; // rdx
  __int64 offset_1; // rax
  QWORD *secretAppend; // r8
  QWORD *appendEnd; // rdx
  __int64 offset; // rax
  DWORD v28; // esi
  UCHAR *v29; // rsi
  signed __int64 cbDerivedKey; // rsi
  const WCHAR *v31; // rdx
  __int64 v32; // rax
  __int64 v33; // rax
  const char *v35; // [rsp+40h] [rbp-C0h] BYREF
  int v36; // [rsp+48h] [rbp-B8h]
  ULONG pcbResult; // [rsp+50h] [rbp-B0h] BYREF
  int v38; // [rsp+54h] [rbp-ACh]
  BCRYPT_SECRET_HANDLE phAgreedSecret; // [rsp+58h] [rbp-A8h] BYREF
  BCryptBufferDesc pParameterList; // [rsp+60h] [rbp-A0h] BYREF
  PUCHAR *v41; // [rsp+70h] [rbp-90h]
  char v42[24]; // [rsp+78h] [rbp-88h] BYREF
  char v43[64]; // [rsp+90h] [rbp-70h] BYREF
  _BCryptBuffer cryptBuffer; // [rsp+D0h] [rbp-30h] BYREF
  int v45; // [rsp+E0h] [rbp-20h]
  int v46; // [rsp+E4h] [rbp-1Ch]
  QWORD *v47; // [rsp+E8h] [rbp-18h]

  v41 = result;
  v38 = 0;
  v8 = (unsigned int)(*((_DWORD *)this + 4) - 7);
  if ( *((_DWORD *)this + 4) != 7 )
  {
    v8 = (unsigned int)(*((_DWORD *)this + 4) - 8);
    if ( *((_DWORD *)this + 4) != 8 && *((_DWORD *)this + 4) != 9 )
    {
      v35 = ".\\cngasymmetrickey.cpp";
      v36 = 211;
      v9 = cdp::MakeException<cdp::HResultException<-2147467263>,>(
             v43,
             &v35,
             "This asymmetric key doesn't support secret exchange");
      cdp::CdpThrow<cdp::HResultException<-2147467263>>(&v35, v9);
    }
  }
  if ( !*((_QWORD *)this + 3) )
  {
    v35 = ".\\cngasymmetrickey.cpp";
    v36 = 212;
    v10 = cdp::MakeException<std::invalid_argument,>(v42, v8, "No key with which to exchange secrets");
    cdp::CdpThrow<std::invalid_argument>(&v35, v10);
  }
  if ( !*((_BYTE *)this + 32) )
  {
    v35 = ".\\cngasymmetrickey.cpp";
    v36 = 213;
    v11 = cdp::MakeException<std::invalid_argument,>(v42, v8, "Can't exchange secrets with a public-only key");
    cdp::CdpThrow<std::invalid_argument>(&v35, v11);
  }
  if ( *((_DWORD *)this + 4) != (*((unsigned int (__fastcall **)(BCRYPT_KEY_HANDLE *))*remoteKey + 1))(remoteKey) )
  {
    v35 = ".\\cngasymmetrickey.cpp";
    v36 = 214;
    v13 = cdp::MakeException<std::invalid_argument,>(v42, v12, "Algorithms of the public/private keys must match");
    cdp::CdpThrow<std::invalid_argument>(&v35, v13);
  }
  phAgreedSecret = 0i64;
  if ( BCryptSecretAgreement(*((BCRYPT_KEY_HANDLE *)this + 3), remoteKey[3], &phAgreedSecret, 0) < 0 )
  {
    v35 = ".\\cngasymmetrickey.cpp";
    v36 = 218;
    v14 = cdp::MakeException<cdp::HResultException<-2147220479>,>(v43, &v35, "Failed to compute secret");
    cdp::CdpThrow<cdp::HResultException<-2147220479>>(&v35, v14);
  }
  curveType = secretParams->curveType;
  if ( secretParams->curveType )
  {
    if ( secretParams->curveType == 1 )
    {
      hashAlgorithm = L"SHA384";
    }
    else if ( secretParams->curveType == 2 )
    {
      hashAlgorithm = L"SHA512";
    }
    else
    {
      hashAlgorithm = (wchar_t *)&pNodeName;
    }
  }
  else
  {
    hashAlgorithm = (wchar_t *)L"SHA256";
  }
  v17 = 64i64;
  memset_0(&cryptBuffer, 0, 0x40ui64);
  strLen = -1i64;
  do
    ++strLen;
  while ( hashAlgorithm[strLen] );
  cryptBuffer.cbBuffer = 2 * strLen + 2;
  cryptBuffer.pvBuffer = hashAlgorithm;
  bufferCount = 1;
  hmacSalt = secretParams->hmacSalt;
  hmacSaltEnd = secretParams->hmacSaltEnd;
  if ( hmacSalt != hmacSaltEnd )
  {
    v46 = 3;
    v45 = (_DWORD)hmacSaltEnd - (_DWORD)hmacSalt;
    v47 = hmacSalt;
    bufferCount = 2;
  }
  secretPrepend = secretParams->prepend;
  prependEnd = secretParams->prependEnd;
  if ( secretPrepend != prependEnd )
  {
    offset_1 = 2i64 * bufferCount;
    *(&cryptBuffer.BufferType + 2 * offset_1) = 1;
    *(&cryptBuffer.cbBuffer + 2 * offset_1) = (_DWORD)prependEnd - (_DWORD)secretPrepend;
    *((_QWORD *)&cryptBuffer.pvBuffer + offset_1) = secretPrepend;
    ++bufferCount;
  }
  secretAppend = secretParams->append;
  appendEnd = secretParams->appendEnd;
  if ( secretAppend != appendEnd )
  {
    offset = 2i64 * bufferCount;
    *(&cryptBuffer.BufferType + 2 * offset) = 2;
    *(&cryptBuffer.cbBuffer + 2 * offset) = (_DWORD)appendEnd - (_DWORD)secretAppend;
    *((_QWORD *)&cryptBuffer.pvBuffer + offset) = secretAppend;
    ++bufferCount;
  }
  pParameterList.ulVersion = 0;
  pParameterList.cBuffers = bufferCount;
  pParameterList.pBuffers = &cryptBuffer;
  if ( curveType )
  {
    v28 = curveType - 1;
    if ( v28 )
    {
      if ( v28 != 1 )
        v17 = 0i64;
    }
    else
    {
      v17 = 48i64;
    }
  }
  else
  {
    v17 = 32i64;
  }
  *result = 0i64;
  result[1] = 0i64;
  result[2] = 0i64;
  v29 = 0i64;
  if ( v17 )
  {
    std::vector<unsigned char,std::allocator<unsigned char>>::_Buy_nonzero(result, v17, secretAppend, 2i64);
    v29 = &(*result)[v17];
    memset_0(*result, 0, v17);
    result[1] = v29;
  }
  v38 = 1;
  pcbResult = 0;
  cbDerivedKey = v29 - *result;
  v31 = L"HASH";
  if ( secretParams->hmacSalt != secretParams->hmacSaltEnd )
    v31 = L"HMAC";
  if ( BCryptDeriveKey(phAgreedSecret, v31, &pParameterList, *result, cbDerivedKey, &pcbResult, 0) < 0 )
  {
    v35 = ".\\cngasymmetrickey.cpp";
    v36 = 264;
    v32 = cdp::MakeException<cdp::HResultException<-2147220479>,>(v43, &v35, "Failed to derive key");
    cdp::CdpThrow<cdp::HResultException<-2147220479>>(&v35, v32);
  }
  if ( result[1] - *result != pcbResult )
  {
    v35 = ".\\cngasymmetrickey.cpp";
    v36 = 266;
    v33 = cdp::MakeException<cdp::HResultException<-2147220479>,>(v43, &v35, "Secret is not the correct length");
    cdp::CdpThrow<cdp::HResultException<-2147220479>>(&v35, v33);
  }
  if ( phAgreedSecret )
    BCryptDestroySecret(phAgreedSecret);
  return result;
}
```
