import json
import os

json_path = 'HraBot.Api/Data/intercom_help_files.json'
data_dir = 'HraBot.Api/Data'

with open(json_path, 'r') as f:
    data = json.load(f)

files = set(os.listdir(data_dir))
count = 0
for entry in data:
    if not entry.get('renamed', False) and count < 30:
        old = entry['blogPostTitle'].strip() + '.md'
        if 'articles/' in entry['link']:
            new = entry['link'].split('articles/')[1] + '.md'
            old_path = os.path.join(data_dir, old)
            new_path = os.path.join(data_dir, new)
            if old in files and not os.path.exists(new_path):
                print(f'Renaming: {old} -> {new}')
                os.rename(old_path, new_path)
                entry['renamed'] = True
                count += 1

with open(json_path, 'w') as f:
    json.dump(data, f, indent=2)
print(f'Renamed {count} files and updated JSON.')
