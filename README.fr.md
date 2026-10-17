# ACR Designer

[English](README.md) | [日本語](README.ja.md) | Français

ACR Designer est une application de bureau permettant de créer et de modifier des définitions de rapports ACR (AcrossReport) au format JSON.

## Fonctionnalités

- Conception de rapports (placement et réglage des bandes et des contrôles, format de papier)
- Choix entre Section et Free Canvas lors de la création d'un nouveau rapport
- Enregistrement et chargement des modèles en JSON (les JSON créés avec [ACR Generator](https://github.com/acrossreport/acr-generator) peuvent aussi être ouverts)
- Connexion aux bases de données : SQLite / SQL Server / PostgreSQL / MySQL / Oracle / Access (Access sous Windows uniquement)
- Aperçu et export en PDF, PNG et HTML
- Japonais / English / Français

## Configuration requise

| OS | Statut |
|---|---|
| Windows x64 | Pris en charge |
| Windows ARM64 | Pris en charge |
| macOS (Apple Silicon) | Pris en charge |
| macOS (Intel) | Pris en charge |
| Linux x64 | Pris en charge |
| Linux ARM64 | Pris en charge |

- Versions de Windows prises en charge : Windows 11
- Le runtime .NET est inclus ; aucune installation séparée n'est nécessaire
- Les versions macOS sont signées avec un Developer ID et notariées par Apple

## Téléchargement

Téléchargez le fichier correspondant à votre OS depuis les [Releases](https://github.com/acrossreport/acr-designer/releases).

- Windows x64 : `AcrossReportDesigner-v0.1.2-win-x64.zip` (modèles et données d'exemple inclus)
- Windows ARM64 : `AcrossReportDesigner-v0.1.2-win-arm64.zip` (modèles et données d'exemple inclus)
- macOS (Apple Silicon) : `AcrossReportDesigner-v0.1.2-osx-arm64.zip`
- macOS (Intel) : `AcrossReportDesigner-v0.1.2-osx-x64.zip`
- Linux x64 : `AcrossReportDesigner-v0.1.2-linux-x64.zip`
- Linux ARM64 : `AcrossReportDesigner-v0.1.2-linux-arm64.zip` (modèles et données d'exemple inclus)

> **Remarque (macOS) :** les paramètres, modèles, données et sorties sont enregistrés dans `~/Desktop/AcrossReportDesigner`. La version macOS n'inclut pas encore les fichiers d'exemple.
> **Remarque (Linux) :** la version Linux x64 n'inclut pas encore les fichiers d'exemple. La version Linux ARM64 les inclut.

## Installation et lancement

1. Extrayez le fichier zip téléchargé dans le dossier de votre choix
2. Windows : lancez `AcrossReportDesigner.exe` dans le dossier extrait. macOS/Linux : lancez `./AcrossReportDesigner` (sous Linux, il peut être nécessaire d'exécuter d'abord `chmod +x AcrossReportDesigner`)

Au premier lancement, le dossier `Output` (`PDF`, `PNG`, `html`) et d'autres dossiers sont créés automatiquement à côté de l'exécutable (sous macOS uniquement, dans `~/Desktop/AcrossReportDesigner`).

## Utilisation

1. Créez un nouveau rapport (Section ou Free Canvas) ou ouvrez un modèle existant (JSON)
2. Placez les bandes et les contrôles, puis réglez leurs propriétés
3. Si nécessaire, connectez-vous à une base de données et affichez l'aperçu avec les données
4. Exportez en PDF, PNG ou HTML (dossier `Output` à côté de l'exe)
5. Enregistrez le modèle en JSON

## À propos de l'export

ACR Designer est destiné à la vérification des rapports : les exports PDF et PNG comportent toujours un filigrane, que vous soyez enregistré ou non. Pour l'impression et l'export en production, utilisez ACR Engine ou ACR Viewer. Consultez le [site officiel](https://acrossreport.com) pour plus de détails.

## Liens

- ACR Samples (modèles/données d'exemple, à essayer tout de suite) : https://github.com/acrossreport/acr-data-samples
- ACR Generator : https://github.com/acrossreport/acr-generator
- Spécification ACR (modèle JSON) : https://github.com/acrossreport/acr-spec
- Site officiel : https://acrossreport.com

## Licence

Le code source de ce logiciel n'est pas public. Veuillez consulter le fichier [LICENSE](LICENSE) pour les conditions d'utilisation.

## Contact

across.support@gmail.com

---

© Across Systems Corporation
L'architecture d'instructions de dessin intermédiaires d'ACR fait l'objet d'une demande de brevet.
