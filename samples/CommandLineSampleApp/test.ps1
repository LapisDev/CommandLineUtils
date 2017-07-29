function dr { dotnet run -- $args }

# math
dr math add 1 2    #=> 3
dr math subtract 5 3   #=> 2
dr math round 1.56 1   #=> 1.6
dr math round 1.56 #=> 2
dr math log 10 -b 10  #=> 1
dr math log 10 #=> 2.30258509299405

# text
dr text equal abc ABC  #=> False
dr text equal abc ABC --ignore-case    #=> True
dr text length 123456  #=> 6
dr text length #=> 0
dr text match "(?:#|0x)?(?:[0-9A-F]{2}){3,4}" `
    "Tomato (#FF6347). RGB value is (255,99,71). " `
    #=> #FF6347
    
# file
dr file head "test_data/text.txt" -n 3
dr file head "test_data/text.txt"
dr file base-64 "test_data/text.txt"