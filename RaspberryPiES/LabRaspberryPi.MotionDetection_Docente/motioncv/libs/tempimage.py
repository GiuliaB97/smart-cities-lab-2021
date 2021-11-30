import uuid
import os

class TempImage:
	def __init__(self, basePath="./", ext=".jpg"):
		# costruzione file path dinamico
		self.path = "{base_path}/{rand}{ext}".format(base_path=basePath,
			rand=str(uuid.uuid4()), ext=ext)

	def cleanup(self):
		# rimozione del file
		os.remove(self.path)
