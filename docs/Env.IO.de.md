# Lua Environment (IO)

Das Lua-Package IO kann in der von lua.org vorgesehenen Form nicht verwendet
werden. Dies liegt zum einem an der asynchronen Verarbeitung der
Befehle und zum anderen wird das Transaktionssystem des DES unters�tzt.

Es werden folgende Funktionen aus dem Standard unterst�tzt.

open(filename, mode, encoding)
:   �ffnet eine Datei und gibt ein Datei-Handle zur�ck, welches sich 
    dann wieder Standardkonform verh�lt. `mode` wurde um die Flags `t` und `m`
    erweitert. `t` entscheidet, ob die Datei an eine Transaktion
    gebunden wird und `m`, ob dies im Speicher oder im Dateisystem erfolgen 
    soll. `encoding` gibt die Kodierung von Textinhalten an.

tmpfilenew()
:   Enspricht dem Standard.

Neu hinzu kommen.

openraw(filename, inMemory)
:   �ffnen eine Datei im Transaktionsscope und gibt Zugriff auf den Stream.
   `inMemory` steuert, ob die Transaktionsinformation im Speicher oder
   im Dateisystem erfolgen sollen.

copy(source, dest, throwException, targetExists)
:   Kopiert eine Datei innerhalb einer Transaktion.

move(source, dest, throwException, targetExists)
:   Bewegt eine Datei innerhalb einer Transaktion.

delete(file, throwException)
:   L�scht eine Datei innerhalb einer Transaktion.