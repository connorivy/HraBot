import json
import os
import re

json_path = 'HraBot.Api/Data/intercom_help_files.json'
data_dir = 'HraBot.Api/Data'

with open(json_path, 'r') as f:
    data = json.load(f)

files = set(os.listdir(data_dir))
# create a map of cleaned file names to original file names
file_map = {re.sub(r'[^a-zA-Z0-9 ]', '', entry['blogPostTitle']).strip() + '.md': entry for entry in data}
count = 0
for entry in data:
    if not entry.get('renamed', False) and count < 30:
        old = re.sub(r'[^a-zA-Z0-9 ]', '', entry['blogPostTitle']).strip() + '.md'
        if 'articles/' in entry['link']:
            new = entry['link'].split('articles/')[1] + '.md'
            new_path = os.path.join(data_dir, new)
            if old in file_map.keys() and not os.path.exists(new_path):
                old_path = os.path.join(data_dir, file_map[old])
                print(f'Renaming: {old} -> {new}')
                os.rename(old_path, new_path)
                entry['renamed'] = True
                count += 1

with open(json_path, 'w') as f:
    json.dump(data, f, indent=2)
print(f'Renamed {count} files and updated JSON.')
