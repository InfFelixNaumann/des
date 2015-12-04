DEServer
========

# Einleitung
DES ist ein Dienst der die Kommunikation in einer Firma verwaltet.
Er ist nicht als Server f�r millionen von Anfragen ausgelegt, sondern seine St�rke ist die Verwaltung von definierten Nutzer- und Ger�tegruppen in einen definierten Umfeld. Aber im bescheidenen Umfeld ist auch m�glich �berbetriebliche Aufgaben zu �bernehmen.

## Was es ist
* Innerbetrieblicher Kommunikations Hub
* Hierarchisch Konfigurierbar
* Flexibel erweiterbar
* Externe Datenschnittstellen

## Was es nicht ist
* Ein WebServer f�r den Internetauftritt

# Erste Schritte
1. Um den Dienst starten zu k�nnen, ben�tigt man eine Konfigurationsdatei (Minimalbeispiel):
'''xml
<?xml version="1.0" encoding="utf-8" ?>
<des xmlns="http://tecware-gmbh.de/dev/des/2014" version="330">
	<server logpath="Log" />
	<http />
	<luaengine />
</des>
'''
2. Nun kann der Dienst �ber die Kommandozeile gestartet werden:
'''PS
DEServer.exe run -v -c C:\Config.xml
'''
3. Weiterf�hrende Hilfe zu den Parametern des Dienstes bekommt man mit folgendem Befehl:
'''PS
DEServer.exe help
'''
Bei erfolgreicher Konfiguration des Dienstes kann der Status �ber http://localhost:8080/des.html abgerufen werden.

# Technologie
Grundlegend werden folgende Technologien vorausgesetzt, es kann ja nach Konfiguration zus�tzliches hinzukommen
* Es handelt sich um einen Windows Service
* .net Framework 4.6 (C#)
* Lua f�r Scripting [NeoLua](http://https://github.com/neolithos/neolua)
* http WebServer [HttpSys](https://msdn.microsoft.com/en-us/library/windows/desktop/aa364510%28v=vs.85%29.aspx)

# Mitarbeit
(ToDo)

# Lizenz
(ToDo)

# How-to
(ToDo)