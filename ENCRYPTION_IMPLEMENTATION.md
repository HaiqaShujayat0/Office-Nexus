# Bank Information Encryption Implementation

## Overview
This document describes the AES-256 encryption implementation for sensitive bank information (IBAN, CNIC, Account Number, Account Title) in the OfficeNexus application.

## Architecture

### Data Flow
1. **User Input (Plain Text)** → Validation → **Encryption** → **Database (Encrypted Base64)**
2. **Database (Encrypted Base64)** → **Decryption** → **User View (Plain Text)**

### Key Components

#### 1. SecurityHelper.cs (`Helpers/SecurityHelper.cs`)
- Static helper class providing AES-256 encryption/decryption
- Uses `System.Security.Cryptography.Aes`
- Key: 32 bytes (256 bits) for AES-256
- IV: 16 bytes (128 bits)
- Mode: CBC (Cipher Block Chaining)
- Padding: PKCS7
- Output: Base64-encoded strings for safe database storage

**⚠️ PRODUCTION NOTE:** The encryption key and IV are currently hardcoded. In production, these MUST be:
- Stored in Azure Key Vault, AWS Secrets Manager, or similar
- Loaded from `appsettings.json` with User Secrets (development)
- Never committed to source control
- Rotated periodically

#### 2. Model Updates (`Models/UserBankAccount.cs`)
- Removed `StringLength` constraints from encrypted fields:
  - `AccountTitle` (was 100, now unlimited)
  - `IBAN` (was unlimited, remains unlimited)
  - `AccountNumber` (was 20, now unlimited)
  - `CNIC` (was unlimited, remains unlimited)
- Kept validation attributes (`Required`, `RegularExpression`) for user input validation
- Validation occurs on plain text BEFORE encryption

#### 3. Controller Logic (`Controllers/EmployeeController.cs`)

**GET BankDetails (Decryption Layer):**
```csharp
// Decrypt sensitive fields before displaying to user
model.AccountTitle = SecurityHelper.Decrypt(bankAccount.AccountTitle);
model.IBAN = SecurityHelper.Decrypt(bankAccount.IBAN);
model.CNIC = SecurityHelper.Decrypt(bankAccount.CNIC);
if (!string.IsNullOrEmpty(bankAccount.AccountNumber))
{
    model.AccountNumber = SecurityHelper.Decrypt(bankAccount.AccountNumber);
}
```

**POST BankDetails (Encryption Layer):**
```csharp
// Encrypt sensitive fields before saving to database
string encryptedAccountTitle = SecurityHelper.Encrypt(model.AccountTitle.Trim().ToUpper());
string encryptedIban = SecurityHelper.Encrypt(cleanedIban);
string encryptedCnic = SecurityHelper.Encrypt(model.CNIC.Trim());
string? encryptedAccountNumber = string.IsNullOrWhiteSpace(model.AccountNumber) 
    ? null 
    : SecurityHelper.Encrypt(model.AccountNumber.Trim());
```

#### 4. Admin Reports (`Controllers/AdminController.cs`)
- **No changes required** - Admin BankStatus report only checks record existence (`bg != null`)
- Does NOT decrypt data (privacy constraint)
- Works seamlessly with encrypted data

## Database Schema

### SQLite (Current)
- SQLite `TEXT` columns are unlimited by default
- No migration needed for column length changes
- Migration file created for documentation purposes

### Production Databases (SQL Server, PostgreSQL, etc.)
If migrating to SQL Server or other databases, update columns to:
- `NVARCHAR(MAX)` or `VARCHAR(MAX)` for encrypted fields
- See migration file comments for SQL Server example

## Encrypted Fields
- ✅ **IBAN** - Encrypted
- ✅ **CNIC** - Encrypted
- ✅ **AccountNumber** - Encrypted (if provided)
- ✅ **AccountTitle** - Encrypted
- ❌ **BankName** - NOT encrypted (not sensitive PII)
- ❌ **BranchCode** - NOT encrypted (not sensitive PII)

## Security Considerations

### Encryption Strength
- Algorithm: AES-256 (Advanced Encryption Standard, 256-bit key)
- Industry standard, FIPS 140-2 compliant
- Suitable for sensitive financial data

### Key Management (TODO for Production)
1. Move keys to secure storage (Azure Key Vault, AWS Secrets Manager)
2. Implement key rotation strategy
3. Use different keys per environment (dev/staging/prod)
4. Never log or expose keys in error messages

### Error Handling
- Decryption errors are caught and handled gracefully
- If decryption fails (corrupted data or old unencrypted data), user sees empty fields with error message
- Consider implementing a data migration script for existing unencrypted records

## Migration Path for Existing Data

If you have existing unencrypted bank account records:

1. **Option A: One-time Migration Script**
   - Create a script that reads all existing records
   - Encrypts the plain text fields
   - Updates the database

2. **Option B: Lazy Migration**
   - When a user views their bank details, check if data is encrypted
   - If not encrypted, encrypt it on-the-fly and save
   - Gradually migrate all records

3. **Option C: Manual Re-entry**
   - Ask users to re-enter their bank details
   - New entries will be encrypted automatically

## Testing Checklist

- [x] New bank details are encrypted when saved
- [x] Existing bank details are decrypted when viewed
- [x] Admin Bank Status report still works (checks existence only)
- [x] Validation works on plain text before encryption
- [x] Error handling for decryption failures
- [ ] Test with existing unencrypted data (if applicable)
- [ ] Test key rotation (production)

## Files Modified

1. `Helpers/SecurityHelper.cs` - NEW - Encryption/decryption helper
2. `Models/UserBankAccount.cs` - Removed StringLength constraints
3. `Controllers/EmployeeController.cs` - Added encryption/decryption hooks
4. `Migrations/20251202000000_EncryptBankAccountFields.cs` - NEW - Migration documentation

## Files NOT Modified (Intentionally)

- `Controllers/AdminController.cs` - No changes needed (only checks existence)
- `Views/Admin/BankStatus.cshtml` - No changes needed (doesn't display sensitive data)
- `Views/Employee/BankDetails.cshtml` - No changes needed (receives decrypted data from controller)

## Next Steps

1. **Production Deployment:**
   - Move encryption keys to secure storage (Azure Key Vault, etc.)
   - Update `SecurityHelper.cs` to load keys from configuration
   - Test key rotation process

2. **Data Migration (if needed):**
   - Create migration script for existing unencrypted records
   - Test migration on staging environment
   - Schedule migration window

3. **Monitoring:**
   - Add logging for encryption/decryption operations (without logging keys)
   - Monitor for decryption failures
   - Set up alerts for encryption errors

## Support

For issues or questions:
- Check error logs for decryption failures
- Verify encryption keys are correctly configured
- Ensure database columns can accommodate encrypted data length

