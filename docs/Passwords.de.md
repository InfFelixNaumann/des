# Passwords

Passw�rter k�nnen in 3 verschiedenen formen in der Konfiguration abgelegt werden.

+----------+------------------------------------------+
| Prefix   | Beschreibung                             |
+----------+------------------------------------------+
| `win0x`  | Das Password wird verschl�sselt und kann |
|          | nur von der lokalen Maschine wieder      |
| `win64`  | entschl�sselt werden.                    |
+----------+------------------------------------------+
| `usr0x`  | Das Password wird verschl�sselt und kann |
|          | nur von dem aktuellen Nutzer wieder      |
| `usr64`  | entschl�sselt werden.                    |
+----------+------------------------------------------+
| `plain`  | Das Passwort wird in klartext verwendet. |
+----------+------------------------------------------+

Das `0x` und `64` stehen f�r die Kodierung der Bytefolge. Einmal Hexadezimal bzw. Base64.

Die Hexadezimalkodierung kann optional das prefix `0x` haben.


## Lua-Funktionen

Es gibt die zwei funktionen zum en/decodieren des Passwortes.
- EncodePassword(password, passswordType = "win64")
- DecodePassword(passwordValue)
- DecodePasswordSecure(passwordValue)
- 
```Lua
return DecodePassword("win0x:" + password);
```

## Passw�rte via Powershell erzeugen

Ein "lokal Machine"-Passwort kann �ber die Powershell generiert werden.

```PS
 "win0x:" + (ConvertFrom-SecureString -SecureString (Read-Host -AsSecureString -Prompt "Passwort"))
```

# Passwort-Hash

Bei einem Passwort-Hash wird nur die Pr�fsumme des Passwortes abgelegt. D.h.
es k�nnen nur Passw�rter dagegen gepr�ft werden.

Der Hash kann ein Hex-Byte folge sein (prefix `0x`) oder Base64 enkodiert.

Der MS-SQL-Server verwendet den selben Algorithmus, dadurch kann ein Passwort mittels erzeugt werden.

```sql
select PWDENCRYPT('test')
```

## Lua-Funktionen

- EncodePasswordHash(password)
- ComparePasswordHash(string password, string passwordHash)
