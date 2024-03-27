#include <iostream>
#include <fstream>
#include <cstdlib>
#include <cstring>
#include <sys/stat.h>
#include <sys/types.h>
#include <vector>
#include <algorithm>
#include <istream>
#include <string>
#include <windows.h>

using namespace std;

constexpr char* pathSeparator = ";";
constexpr char* dirSeparator = "\\";
constexpr char* azdBinaryFileName = "azd.exe";
constexpr char* azdFullNameCacheFile = "azd.ini";
constexpr char* azdFullNameMustContains = "Azure Dev CLI";

constexpr char* aiCommandName = "ai";
constexpr char* aiBinaryFileName = "ai.exe";

string findMyself() {
    char path[MAX_PATH];
    GetModuleFileNameA(NULL, path, MAX_PATH);

    auto whereThisBinaryLives = string(path);
    auto lastSlash = whereThisBinaryLives.find_last_of(dirSeparator);
    if (lastSlash != string::npos) {
        whereThisBinaryLives = whereThisBinaryLives.substr(0, lastSlash);
    }
    else {
        whereThisBinaryLives = ".";
    }

    return whereThisBinaryLives;
}

bool fileExists(const string& name) {
    struct stat buffer;
    return (stat (name.c_str(), &buffer) == 0);
}

string findAzdBinaryInPath() {
    vector<string> paths;
    char* path = getenv("PATH");
    char* token = strtok(path, pathSeparator);
    while (token != NULL) {
        paths.push_back(string(token));
        token = strtok(NULL, pathSeparator);
    }

    for (auto& p : paths) {
        string withPath = p + std::string(dirSeparator) + azdBinaryFileName;
        if (fileExists(withPath) && withPath.find(azdFullNameMustContains) != string::npos) {
            return withPath;
        }
    }

    printf("ERROR: `%s` not found in PATH!!\n\n  TRY: Update PATH to include location %s...\n   OR: Update %s with full path to %s", azdBinaryFileName, azdBinaryFileName, azdFullNameCacheFile, azdBinaryFileName);
    exit(1);
}

string findAndCacheAzdBinary()
{
    auto whereThisBinaryLives = findMyself();

    auto sideBySideCacheFile = whereThisBinaryLives + dirSeparator + azdFullNameCacheFile;
    ifstream cacheStream(sideBySideCacheFile);

    string cachedContent;
    if (cacheStream.good()) {
        getline(cacheStream, cachedContent);
        cacheStream.close();
    } else {
        cachedContent = findAzdBinaryInPath();
        std::ofstream outFile(sideBySideCacheFile);
        outFile << cachedContent;
        outFile.close();
    }

    return cachedContent;
}

string quoteArgIfNeeded(const char* arg) {

    bool needsQuoted = false;
    string argStr(arg);

    if (argStr.find('\"') != string::npos) {
        needsQuoted = true;
        size_t pos = 0;
        while ((pos = argStr.find("\"", pos)) != string::npos) {
            argStr.replace(pos, 1, "\\\"");
            pos += 2;
        }
    }

    if (argStr.find(' ') != string::npos) {
        needsQuoted = true;
    }

    return needsQuoted
        ? "\"" + argStr + "\""
        : argStr;
}

int main(int argc, char* argv[]) {

    auto actualBinaryFullFileName = findAndCacheAzdBinary();

    if (argc > 1 && string(argv[1]) == aiCommandName) {
        string commandLine(aiBinaryFileName);
        for (int i = 2; i < argc; i++) {
            commandLine += " ";
            commandLine += quoteArgIfNeeded(argv[i]);
        }
        return system(commandLine.c_str());
    } else {
        string commandLine("\"" + actualBinaryFullFileName + "\"");
        for (int i = 1; i < argc; i++) {
            commandLine += " ";
            commandLine += quoteArgIfNeeded(argv[i]);
        }
        return system(commandLine.c_str());
    }
}