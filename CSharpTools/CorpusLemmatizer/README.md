# Консольная утилита для лемматизации текстового корпуса

Назначение данной утилиты - привести слова в заданном текстовом файле к нормальным (словарным)
формам, леммам. Для этого используется заранее обученная вероятностная модель русской морфологии.

## Файлы модели частеречной разметки

Для использования утилиты необходимо распаковать архив в каталоге https://github.com/Koziev/NGrams/tree/master/CSharpTools/POSTaggerModel
Из-за ограничений репозитория архив разбит на куски размером 10 Мб, соответственно надо
распаковать этот многотомный архив.

## Запуск лемматизации

В командной строке:

CorpusLemmatizer.exe путь_к_словарю  путь_к_входному_тексту  путь_к_файлу_с_результатами

Путь к словарю - это путь к файлу dictionary.xml, находящемуся в подкаталоге https://github.com/Koziev/NGrams/tree/master/CSharpTools/POSTaggerModel.

Пример:

'''
f:\Parser\dictionary.xml f:\Corpus\Raw\ru\text_blocks.txt f:\Corpus\lemmas\ru\raw\raw_lemmatized.txt
'''
