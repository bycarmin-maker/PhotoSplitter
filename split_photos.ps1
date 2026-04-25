$source_dir = "D:\[뉴진스(엔제이지) NewJeans(NJZ)]\개인저장 뉴진스 사진\휴대폰\뉴진스"
$target_base_dir = $source_dir
$chunk_size = 500

$valid_extensions = @('.jpg', '.jpeg', '.png', '.heic', '.heif', '.gif', '.bmp', '.webp', '.tiff')

Write-Host "원본 폴더 스캔 중... 경로: $source_dir"

if (-not (Test-Path $source_dir)) {
    Write-Host "오류: 원본 폴더를 찾을 수 없습니다."
    exit
}

# 파일 목록 수집 (하위 폴더 제외, 확장자 필터링)
$image_files = Get-ChildItem -Path $source_dir -File | Where-Object { $valid_extensions -contains $_.Extension.ToLower() }

if ($null -eq $image_files) {
    $total_files = 0
} elseif ($image_files -is [array]) {
    $total_files = $image_files.Count
} else {
    $total_files = 1
    $image_files = @($image_files)
}

Write-Host "총 $total_files 장의 사진 파일을 찾았습니다."

if ($total_files -eq 0) {
    Write-Host "복사할 사진 파일이 없습니다."
    exit
}

$total_folders = [math]::Ceiling($total_files / $chunk_size)

for ($i = 0; $i -lt $total_folders; $i++) {
    $folder_num = $i + 1
    $folder_name = "사진분할_{0:D2}" -f $folder_num
    $target_dir = Join-Path -Path $target_base_dir -ChildPath $folder_name
    
    if (-not (Test-Path $target_dir)) {
        New-Item -ItemType Directory -Path $target_dir | Out-Null
    }
    
    $start_idx = $i * $chunk_size
    $end_idx = [math]::Min(($i + 1) * $chunk_size, $total_files) - 1
    
    $current_chunk = $image_files[$start_idx..$end_idx]
    
    foreach ($file in $current_chunk) {
        $dst_path = Join-Path -Path $target_dir -ChildPath $file.Name
        Copy-Item -Path $file.FullName -Destination $dst_path
    }
    
    Write-Host "현재 ${folder_num}번째 폴더(${folder_name})에 $($current_chunk.Count)장 복사 완료"
}

Write-Host "`n모든 복사 작업이 안전하게 완료되었습니다!"
