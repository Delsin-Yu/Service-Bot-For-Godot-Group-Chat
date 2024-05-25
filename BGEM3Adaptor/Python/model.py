import json
from FlagEmbedding import BGEM3FlagModel

print("warm up...")

model = BGEM3FlagModel('BAAI/bge-m3', use_fp16=True)

def embedding(sentence : str):
	return model.encode(sentence)['dense_vecs']