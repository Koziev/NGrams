# -*- coding: utf-8 -*-
'''
'''

from __future__ import print_function
import logging
import os
import codecs
import tqdm
import sys

def match1( prefix, word ):
    return word.startswith(prefix) and len(word)>=len(prefix)

def does_match( prefix1, prefix2, w1, w2 ):
    return (match1(prefix1,w1) and match1(prefix2,w2)) or (match1(prefix1, w2) and match1(prefix2, w1));


data_path = r'f:\tmp\assoc_2_ru.dat'

nb_lines = 0
with codecs.open(data_path, 'r', 'utf-8') as rdr:
    for line in rdr:
        nb_lines+=1


while True:
    words = raw_input('words: ').decode(sys.stdout.encoding).strip().lower().split(u' ')
    if len(words)==2:
        word1 = words[0]
        word2 = words[1]

        matchings = []

        with codecs.open(data_path, 'r', 'utf-8') as rdr:
            for line in tqdm.tqdm(rdr, total=nb_lines, desc="Matching"):
                pwords = line.strip().split(u'\t')
                if len(pwords)==3:
                    w1 = pwords[0]
                    w2 = pwords[1]
                    if does_match(word1, word2, w1, w2):
                        matchings.append(pwords)

        print('{} records matched:'.format(len(matchings)))
        for m in matchings:
            print(u'{}\t{}\t==>{}'.format(m[0], m[1], m[2]))
