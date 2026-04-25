import os
import shutil
import math

def get_valid_input(prompt, default_val=None, is_int=False):
    while True:
        user_input = input(prompt).strip()
        if not user_input and default_val is not None:
            return default_val
        if is_int:
            try:
                val = int(user_input)
                if val <= 0:
                    print("1 이상의 숫자를 입력해주세요.")
                    continue
                return val
            except ValueError:
                print("유효한 숫자를 입력해주세요.")
        else:
            if user_input:
                return user_input
            print("값을 입력해주세요.")

def split_photos():
    print("=" * 50)
    print("사진 파일 분할 프로그램")
    print("=" * 50)
    
    # 1. 경로 입력
    source_dir = get_valid_input("원본 사진들이 있는 폴더 경로를 입력하세요: ")
    # 따옴표 제거 (경로 복사 시 따옴표가 포함될 수 있음)
    source_dir = source_dir.strip('\"\'')
    
    if not os.path.exists(source_dir):
        print(f"\n오류: 원본 폴더를 찾을 수 없습니다.\n경로: {source_dir}")
        input("엔터를 누르면 종료됩니다...")
        return

    # 2. 하위 폴더 저장 위치
    target_base_dir = source_dir
    
    # 3. 폴더당 파일 개수 입력
    chunk_size = get_valid_input("한 폴더당 들어갈 사진의 개수를 입력하세요 (기본값: 500): ", default_val=500, is_int=True)
    
    # 4. 폴더명 입력
    prefix = get_valid_input("생성할 하위 폴더의 이름을 입력하세요 (기본값: 사진분할): ", default_val="사진분할")
    
    # 사진 파일 확장자 필터링
    valid_extensions = {'.jpg', '.jpeg', '.png', '.heic', '.heif', '.gif', '.bmp', '.webp', '.tiff'}
    
    print(f"\n원본 폴더 스캔 중...\n경로: {source_dir}")
    
    # 파일 목록 수집
    image_files = []
    try:
        for filename in os.listdir(source_dir):
            file_path = os.path.join(source_dir, filename)
            if os.path.isfile(file_path):
                ext = os.path.splitext(filename)[1].lower()
                if ext in valid_extensions:
                    image_files.append(filename)
    except PermissionError:
        print("오류: 원본 폴더에 접근할 권한이 없습니다.")
        input("엔터를 누르면 종료됩니다...")
        return
    except FileNotFoundError:
        print("오류: 원본 폴더 경로가 잘못되었습니다.")
        input("엔터를 누르면 종료됩니다...")
        return
                
    total_files = len(image_files)
    print(f"\n총 {total_files}장의 사진 파일을 찾았습니다.")
    
    if total_files == 0:
        print("복사할 사진 파일이 없습니다.")
        input("엔터를 누르면 종료됩니다...")
        return
        
    total_folders = math.ceil(total_files / chunk_size)
    print(f"총 {total_folders}개의 폴더로 나누어 복사를 시작합니다...\n")
    
    for i in range(total_folders):
        folder_num = i + 1
        folder_name = f"{prefix}_{folder_num:02d}"
        target_dir = os.path.join(target_base_dir, folder_name)
        
        if not os.path.exists(target_dir):
            os.makedirs(target_dir)
            
        start_idx = i * chunk_size
        end_idx = min((i + 1) * chunk_size, total_files)
        current_chunk = image_files[start_idx:end_idx]
        
        for filename in current_chunk:
            src_path = os.path.join(source_dir, filename)
            dst_path = os.path.join(target_dir, filename)
            # 원본 파일을 훼손하지 않기 위해 복사(shutil.copy2) 진행
            shutil.copy2(src_path, dst_path)
            
        print(f"현재 {folder_num}번째 폴더({folder_name})에 {len(current_chunk)}장 복사 완료")

    print("\n모든 복사 작업이 안전하게 완료되었습니다!")
    input("프로그램을 종료하려면 엔터를 누르세요...")

if __name__ == "__main__":
    split_photos()
